using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.WindowsServices;
using Recyclarr.Server;
using Recyclarr.Server.Api;
using Recyclarr.Server.Hosting;
using Recyclarr.Server.Logging;
using Recyclarr.Server.Observability;
using Recyclarr.Server.OutboundHttp;
using Recyclarr.Server.Persistence;
using Recyclarr.Server.Resources;
using Recyclarr.Server.Sync.Schedule;
using TickerQ.DependencyInjection;

var builder = WebApplication.CreateSlimBuilder(
    new WebApplicationOptions
    {
        Args = args,
        // A Windows service starts in C:\Windows\System32, which would otherwise be the content
        // root.
        ContentRootPath = WindowsServiceHelpers.IsWindowsService()
            ? AppContext.BaseDirectory
            : null,
    }
);

var launch = ServerLaunchOptions.From(builder.Configuration);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(b =>
    CompositionRoot.Setup(b, launch.LogLevel, launch.Mode)
);

builder.Services.AddServerLogging();
builder.Services.AddOutboundHttpClients();
builder.Services.AddServerTelemetry(builder.Configuration);

// Each takes over the host lifetime only when its service manager launched this process (readiness
// and stop notifications for a systemd unit, the SCM protocol for a Windows service). Under a
// terminal, Docker, or Kubernetes, the default console lifetime remains.
builder.Services.AddSystemd();
builder.Services.AddWindowsService();

builder.Services.AddServerApi();
builder.Services.AddHealthChecks();

if (launch.ParentPid is { } parentPid)
{
    builder.Services.AddHostedService(sp => new StdinLifelineMonitor(
        sp.GetRequiredService<IHostApplicationLifetime>(),
        parentPid
    ));
}

if (!launch.IsDocumentGeneration)
{
    builder.Services.AddServerPersistence();
    builder.Services.AddHostedService<ServerBootstrapService>();
    builder.Services.AddHostedService<SyncScheduler>();
    builder.Services.AddHostedService<ResourceRefreshWorker>();
}

await using var app = builder.Build();

if (launch.Mode == ServerMode.Persistent && !launch.IsDocumentGeneration)
{
    app.UseManagementPort();
}

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
if (!launch.IsDocumentGeneration)
{
    app.UseMiddleware<GuideLeaseMiddleware>();
}

app.UseServerApi();

if (!launch.IsDocumentGeneration)
{
    app.UseTickerQ();
}

return await app.RunServerAsync(launch);
