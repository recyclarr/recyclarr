using System.Diagnostics;
using System.Reflection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Recyclarr.Server.Persistence;

namespace Recyclarr.Server.Telemetry;

/// <summary>
/// OpenTelemetry for the server. OTLP export of traces, metrics, and logs is configured only by
/// the standard <c>OTEL_*</c> environment variables and is active only when
/// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is set. A persistent server also serves metrics to
/// Prometheus on its management port (ADR-029).
/// </summary>
internal static class ServerTelemetry
{
    /// <summary>
    /// Name of Recyclarr's own <see cref="ActivitySource"/> and meter.
    /// </summary>
    public const string SourceName = "Recyclarr";

    private const string DefaultServiceName = "recyclarr-server";

    public static ActivitySource Source { get; } = new(SourceName);

    public static string ServiceVersion { get; } =
        typeof(ServerTelemetry)
            .Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
        ?? "unknown";

    public static bool ExportsOtlp(IConfiguration configuration)
    {
        return !string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
    }

    /// <summary>
    /// <c>OTEL_SERVICE_NAME</c> when set, so an explicit name in code does not override the
    /// standard variable.
    /// </summary>
    public static string ServiceName(IConfiguration configuration)
    {
        return configuration["OTEL_SERVICE_NAME"] is { Length: > 0 } name
            ? name
            : DefaultServiceName;
    }

    extension(IServiceCollection services)
    {
        public void AddServerTelemetry(IConfiguration configuration, ServerMode mode)
        {
            var otlp = ExportsOtlp(configuration);
            if (!otlp && mode == ServerMode.Ephemeral)
            {
                return;
            }

            var telemetry = services
                .AddOpenTelemetry()
                .ConfigureResource(r =>
                    r.AddService(ServiceName(configuration), serviceVersion: ServiceVersion)
                )
                .WithMetrics();

            // server.management.metrics lives in settings.yml, which loads through the container
            // and is not available while services are registered. This callback runs while the
            // meter provider is built, so it can read settings. With Prometheus off and no OTLP,
            // the provider listens to no meters at all. Only builder calls that avoid
            // ConfigureServices work here (e.g. AddPrometheusExporter without options).
            // REC-196 (settings.yml as an IConfiguration provider) makes settings available at
            // registration time and removes the need for this deferral.
            services.ConfigureOpenTelemetryMeterProvider(
                (sp, metrics) =>
                {
                    var management = sp.GetRequiredService<ManagementPort>();
                    var prometheus = management.Number is not null && management.ServesMetrics;
                    if (!otlp && !prometheus)
                    {
                        return;
                    }

                    metrics.AddMeter(
                        "Microsoft.AspNetCore.Hosting",
                        "Microsoft.AspNetCore.Server.Kestrel",
                        "System.Net.Http",
                        SourceName
                    );

                    if (prometheus)
                    {
                        metrics.AddPrometheusExporter();
                    }
                }
            );

            if (!otlp)
            {
                return;
            }

            telemetry
                .WithTracing(tracing =>
                    tracing.AddSource("Microsoft.AspNetCore", "System.Net.Http", SourceName)
                )
                .UseOtlpExporter();
        }
    }
}
