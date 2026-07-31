using System.Text.Json;
using System.Text.Json.Serialization;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using FastEndpoints;
using FastEndpoints.OpenApi;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Recyclarr.Server;
using Scalar.AspNetCore;
using Serilog.Events;

var builder = WebApplication.CreateSlimBuilder(args);

// Present only in ephemeral mode: the CLI passes its own pid and log level, and reads this
// process's stdout for both the READY handshake and forwarded log events (ADR-010).
var parentPid = ParseParentPid(builder.Configuration["parent-pid"]);
var logOptions = new ServerLogOptions(
    ParseLogLevel(builder.Configuration["log-level"]),
    UseParentProtocol: parentPid is not null
);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(b => CompositionRoot.Setup(b, logOptions));
builder.Services.AddSerilog(
    (services, config) => services.GetRequiredService<ServerLogger>().Configure(config)
);
builder.Services.AddOutboundHttpClients();

builder
    .Services.AddFastEndpoints(o =>
    {
        // Endpoints only ever live in this assembly. Auto-discovery reflects over every loaded
        // assembly, which throws when the host process has also loaded one whose dependencies
        // cannot be resolved (FastEndpoints#121).
        o.DisableAutoDiscovery = true;
        o.Assemblies = [typeof(CompositionRoot).Assembly];
    })
    .OpenApiDocument(o =>
    {
        o.MaxEndpointVersion = 1;
        o.DocumentName = "v1";
        o.Title = "Recyclarr API";
        o.Version = "v1";
        o.ShortSchemaNames = true;
        o.EnableJWTBearerAuth = false;

        // Endpoints tag themselves explicitly; path-segment tagging would add a redundant "Api"
        // tag derived from the /api/v1 route prefix.
        o.AutoTagPathSegmentIndex = 0;
    });

// Standalone invocations (e.g. the foreground `serve` command) have no parent to watch and manage
// their own lifecycle via SIGTERM.
if (parentPid is not null)
{
    builder.Services.AddHostedService(sp => new StdinLifelineMonitor(
        sp.GetRequiredService<IHostApplicationLifetime>(),
        parentPid.Value
    ));
}

builder.Services.AddHostedService<ServerBootstrapService>();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseFastEndpoints(c =>
{
    c.Versioning.Prefix = "api/v";
    c.Versioning.PrependToRoute = true;
    c.Errors.UseProblemDetails();
    c.Serializer.Options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    c.Endpoints.NameGenerator = OperationIds.Generate;
});
app.MapOpenApi();
app.MapScalarApiReference();

await app.StartAsync();

// Emit the READY handshake so callers know the port we actually bound to
var server = app.Services.GetRequiredService<IServer>();
var addresses = server.Features.Get<IServerAddressesFeature>();
var boundAddress = addresses?.Addresses.FirstOrDefault();
var port = boundAddress is not null ? new Uri(boundAddress).Port : -1;
app.Services.GetRequiredService<IReadySignal>().Ready(port);

await app.WaitForShutdownAsync();

return;

static int? ParseParentPid(string? value)
{
    return int.TryParse(value, out var pid) ? pid : null;
}

static LogEventLevel ParseLogLevel(string? value)
{
    return Enum.TryParse<LogEventLevel>(value, ignoreCase: true, out var level)
        ? level
        : LogEventLevel.Information;
}
