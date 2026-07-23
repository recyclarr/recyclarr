using Autofac;
using Autofac.Extensions.DependencyInjection;
using FastEndpoints;
using FastEndpoints.OpenApi;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Recyclarr.Server;
using Scalar.AspNetCore;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(CompositionRoot.Setup);

builder
    .Services.AddFastEndpoints()
    .OpenApiDocument(o =>
    {
        o.MaxEndpointVersion = 1;
        o.DocumentName = "v1";
        o.Title = "Recyclarr API";
        o.Version = "v1";
        o.ShortSchemaNames = true;
        o.EnableJWTBearerAuth = false;
    });

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

app.UseFastEndpoints(c =>
{
    c.Versioning.Prefix = "v";
    c.Versioning.PrependToRoute = true;
});
app.MapOpenApi();
app.MapScalarApiReference();

await app.StartAsync();

// Emit the READY handshake so callers know the port we actually bound to
var server = app.Services.GetRequiredService<IServer>();
var addresses = server.Features.Get<IServerAddressesFeature>();
var boundAddress = addresses?.Addresses.FirstOrDefault();
var port = boundAddress is not null ? new Uri(boundAddress).Port : -1;
Console.WriteLine($"READY:{port}");

await app.WaitForShutdownAsync();
