using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using FastEndpoints;
using FastEndpoints.OpenApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.OpenApi;
using Recyclarr;
using Recyclarr.Logging;
using Recyclarr.Server;
using Recyclarr.Server.Auth;
using Recyclarr.Server.Persistence;
using Recyclarr.Server.Sync.Schedule;
using Scalar.AspNetCore;
using Serilog.Events;
using TickerQ.DependencyInjection;

var builder = WebApplication.CreateSlimBuilder(args);

// A CLI that launches this process passes its own pid so the server stops when the CLI does
// (ADR-010). Whether the server is ephemeral is a separate choice: `recyclarr serve` launches a
// persistent server it still owns.
var parentPid = ParseParentPid(builder.Configuration["parent-pid"]);
var mode = builder.Configuration.GetValue<bool>("ephemeral")
    ? ServerMode.Ephemeral
    : ServerMode.Persistent;
var logLevel = ParseLogLevel(builder.Configuration["log-level"]);

// Build-time OpenAPI generation runs this entry point and starts the host, including hosted
// services, against a no-op server. Main cannot exit early (generation needs a built host), so
// services that touch disk, the database, or the config directory are not registered at all.
// https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi
var isDocumentGeneration = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(b => CompositionRoot.Setup(b, logLevel, mode));
builder.Services.AddSerilog(
    (services, config) => services.GetRequiredService<ServerLogger>().Configure(config)
);
builder.Services.AddOutboundHttpClients();

// IResult responses (TypedResults.Json), which endpoints use for Problem Details with extension
// members, serialize with ASP.NET's options rather than FastEndpoints'.
builder.Services.ConfigureHttpJsonOptions(o => ConfigureWireJson(o.SerializerOptions));

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

        // No BearerFormat: the server API key is an opaque token, not a JWT.
        o.AddAuth(
            ApiKeyAuthenticationHandler.SchemeName,
            new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer" }
        );
        o.TagDescriptions = t =>
        {
            t["Guide"] = "Read-only TRaSH Guides data, scoped to one service.";
            t["Instances"] = "Direct calls to a configured Sonarr or Radarr instance.";
            t["Sync"] = "Sync jobs, their progress and results, and the built-in sync schedule.";
        };

        // Endpoints tag themselves explicitly; path-segment tagging would add a redundant "Api"
        // tag derived from the /api/v1 route prefix.
        o.AutoTagPathSegmentIndex = 0;
        o.ConfigureOpenApi = options =>
        {
            options.CreateSchemaReferenceId = PolymorphicSchemaIds.Wrap(
                options.CreateSchemaReferenceId
            );
            // Conceptual prose (overview, job lifecycle, authentication) lives in the wiki.
            options.AddDocumentTransformer(
                (document, _, _) =>
                {
                    document.Info.Description =
                        "HTTP API of the Recyclarr server, which syncs TRaSH Guides to Sonarr "
                        + "and Radarr.";
                    return Task.CompletedTask;
                }
            );
            // FastEndpoints declares 401 with only a reason phrase, and not at all on endpoints
            // that clear default responses. Everything under /api requires the key (ADR-027).
            options.AddDocumentTransformer(
                (document, _, _) =>
                {
                    var operations = document
                        .Paths.Where(p => p.Key.StartsWith("/api/", StringComparison.Ordinal))
                        .SelectMany(p =>
                            p.Value.Operations?.Values ?? Enumerable.Empty<OpenApiOperation>()
                        );

                    foreach (var operation in operations)
                    {
                        operation.Responses ??= [];
                        operation.Responses["401"] = new OpenApiResponse
                        {
                            Description =
                                "The request has no valid server API key in its Authorization "
                                + "header.",
                        };
                    }

                    return Task.CompletedTask;
                }
            );
            options.AddDocumentTransformer<DiscriminatorDocumentTransformer>();
            options.AddSchemaTransformer<EnumDescriptionSchemaTransformer>();
        };
    });

// FastEndpoints requires authentication on every endpoint that does not opt out (ADR-027).
builder
    .Services.AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationHandler.SchemeName,
        configureOptions: null
    );
builder.Services.AddAuthorization();

// Standalone invocations (e.g. the foreground `serve` command) have no parent to watch and manage
// their own lifecycle via SIGTERM.
if (parentPid is not null)
{
    builder.Services.AddHostedService(sp => new StdinLifelineMonitor(
        sp.GetRequiredService<IHostApplicationLifetime>(),
        parentPid.Value
    ));
}

if (!isDocumentGeneration)
{
    builder.Services.AddServerPersistence();
    builder.Services.AddHostedService<ServerBootstrapService>();
    builder.Services.AddHostedService<SyncScheduler>();
}

await using var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints(c =>
{
    c.Versioning.Prefix = "api/v";
    c.Versioning.PrependToRoute = true;
    // FastEndpoints' own ProblemDetails is sealed, so endpoints cannot add RFC 9457 extension
    // members to it (FastEndpoints#637). ASP.NET's type can be subclassed with typed members.
    c.Errors.ProducesMetadataType = typeof(HttpValidationProblemDetails);
    c.Errors.ResponseBuilder = ErrorResponses.Build;
    ConfigureWireJson(c.Serializer.Options);
    c.Endpoints.NameGenerator = OperationIds.Generate;
});
app.MapOpenApi();
app.MapScalarApiReference();

if (!isDocumentGeneration)
{
    app.UseTickerQ();
}

try
{
    if (!isDocumentGeneration)
    {
        // Fail closed: without a key the server would serve its API to anyone (ADR-027).
        app.Services.GetRequiredService<ServerApiKey>();
    }

    await app.StartAsync();
}
catch (Exception e)
    when ((e as FatalException ?? e.FindInnerException<FatalException>()) is { } fatal)
{
    // The cause is already logged; startup fails without a READY line so a parent CLI sees the
    // child exit instead of a server that cannot sync (ADR-019).
    app.Services.GetRequiredService<ILogger>()
        .Fatal("Server startup failed: {Message}", fatal.Message);
    return 1;
}

// Emit the READY handshake so callers know the port we actually bound to
var server = app.Services.GetRequiredService<IServer>();
var addresses = server.Features.Get<IServerAddressesFeature>();
var boundAddress = addresses?.Addresses.FirstOrDefault();
var port = boundAddress is not null ? new Uri(boundAddress).Port : -1;
app.Services.GetRequiredService<IReadySignal>().Ready(port);

await app.WaitForShutdownAsync();

return 0;

static void ConfigureWireJson(JsonSerializerOptions options)
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.RespectNullableAnnotations = true;
    options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
}

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
