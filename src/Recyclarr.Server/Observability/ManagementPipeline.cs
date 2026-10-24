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
        /// pipeline never sees them. Unknown paths in the branch get 404. The port is read per
        /// request: settings.yml loads lazily, and a load failure must surface through the startup
        /// failure path, not while the pipeline is built.
        /// </remarks>
        public void UseManagementPort()
        {
            var port = app.Services.GetRequiredService<ManagementPort>();
            app.MapWhen(
                ctx => ctx.Connection.LocalPort == port.Number,
                management => Configure(management, port)
            );
        }
    }

    private static void Configure(IApplicationBuilder management, ManagementPort port)
    {
        management.Use(SuppressTelemetry);

        var noChecks = new HealthCheckOptions { Predicate = _ => false };
        management.UseHealthChecks("/livez", noChecks);
        management.UseHealthChecks("/readyz", noChecks);

        // The scraping middleware looks up the Prometheus exporter when its pipeline is built, and
        // the exporter exists only when settings enable metrics. settings.yml is not loaded while
        // this pipeline is built, so the metrics pipeline is built on the first scrape instead.
        // REC-196 makes settings available here and removes the deferral.
        RequestDelegate? scrape = null;
        management.Use(
            (ctx, next) =>
            {
                if (ctx.Request.Path != "/metrics" || !port.ServesMetrics)
                {
                    return next(ctx);
                }

                scrape ??= management
                    .New()
                    .UseOpenTelemetryPrometheusScrapingEndpoint(_ => true)
                    .Build();
                return scrape(ctx);
            }
        );

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
