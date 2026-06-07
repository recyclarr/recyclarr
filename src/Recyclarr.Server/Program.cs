using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Recyclarr.Server;
using Scalar.AspNetCore;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(CompositionRoot.Setup);

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer(
        (document, _, _) =>
        {
            document.Info.Title = "Recyclarr API";
            document.Info.Version = "v1";
            return Task.CompletedTask;
        }
    );
});
builder.Services.AddHealthChecks();

// Only activate the lifeline monitor when launched with --parent-pid={pid} (ephemeral mode).
// Standalone invocations (e.g. foreground `serve` command) omit this flag and manage their own
// lifecycle via SIGTERM.
var parentPidStr = builder.Configuration["parent-pid"];
if (parentPidStr is not null && int.TryParse(parentPidStr, out var parentPid))
{
    builder.Services.AddHostedService(sp => new StdinLifelineMonitor(
        sp.GetRequiredService<IHostApplicationLifetime>(),
        parentPid
    ));
}

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();
app.MapHealthChecks("/health");

// Placeholder route group for REC-141 API endpoints
app.MapGroup("/api");

await app.StartAsync();

// Emit the READY handshake so callers know the port we actually bound to
var server = app.Services.GetRequiredService<IServer>();
var addresses = server.Features.Get<IServerAddressesFeature>();
var boundAddress = addresses?.Addresses.FirstOrDefault();
var port = boundAddress is not null ? new Uri(boundAddress).Port : -1;
Console.WriteLine($"READY:{port}");

await app.WaitForShutdownAsync();
