using Serilog.Events;
using Serilog.Filters;

namespace Recyclarr.Server.Logging;

internal static class ServerLoggingRegistration
{
    private static readonly Func<LogEvent, bool> FromHostLifetime = Matching.FromSource(
        "Microsoft.Hosting.Lifetime"
    );

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Routes all events to <see cref="ServerLogger"/>, which owns sinks and enrichment and is
        /// reconfigured once settings.yml has loaded. Only framework filtering happens here.
        /// </summary>
        public void AddServerLogging()
        {
            services.AddSerilog(
                (sp, config) =>
                    config
                        .MinimumLevel.Verbose()
                        .MinimumLevel.Override(
                            "Microsoft.AspNetCore.Hosting.Diagnostics",
                            LogEventLevel.Warning
                        )
                        .Filter.ByExcluding(IsHostListenAddress)
                        .WriteTo.Logger(sp.GetRequiredService<ServerLogger>())
            );
        }
    }

    // The host's "Now listening on" lines cannot say which endpoint is the API and which is
    // management; ServerLifecycle logs labeled lines instead.
    private static bool IsHostListenAddress(LogEvent e)
    {
        return FromHostLifetime(e)
            && e.MessageTemplate.Text.StartsWith("Now listening on", StringComparison.Ordinal);
    }
}
