using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;

namespace Recyclarr.Server.Observability;

internal static class ManagementPipeline
{
    extension(WebApplication app)
    {
        /// <summary>
        /// Serves health probes and metrics on the management port (ADR-029).
        /// </summary>
        /// <remarks>
        /// Requests branch off by the port the connection arrived on, never the Host header, which
        /// clients can forge. Call this before request logging: the branch registers no routing
        /// endpoints, so probes stay out of the request log and the OpenAPI document, and the API
        /// pipeline never sees them. Unknown paths in the branch get 404. Invalid settings give
        /// the default port here; startup then fails before the server listens.
        /// </remarks>
        public void UseManagementPort()
        {
            var port = app.Services.GetRequiredService<ManagementPort>();
            var number = port.Number;
            app.MapWhen(
                ctx => ctx.Connection.LocalPort == number,
                management => Configure(management, port.ServesMetrics)
            );
        }
    }

    private static void Configure(IApplicationBuilder management, bool servesMetrics)
    {
        management.Use(SuppressTelemetry);

        var noChecks = new HealthCheckOptions { Predicate = _ => false };
        management.UseHealthChecks("/livez", noChecks);
        management.UseHealthChecks("/readyz", noChecks);

        // The Prometheus exporter is registered only when settings enable metrics.
        if (servesMetrics)
        {
            management.UseOpenTelemetryPrometheusScrapingEndpoint(ctx =>
                ctx.Request.Path == "/metrics"
            );
        }

        // Routing has already matched any API endpoint for this path; ending the branch without
        // executing it would be a server error rather than a 404.
        management.Run(ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        });
    }

    // Probes and scrapes run every few seconds; tracing or counting them would bury real traffic
    // in exported spans and HTTP server metrics.
    private static Task SuppressTelemetry(HttpContext ctx, RequestDelegate next)
    {
        if (Activity.Current is { } activity)
        {
            activity.IsAllDataRequested = false;
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }

        if (ctx.Features.Get<IHttpMetricsTagsFeature>() is { } metrics)
        {
            metrics.MetricsDisabled = true;
        }

        return next(ctx);
    }
}
