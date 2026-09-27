using Recyclarr.Cli.Console.Helpers;
using Serilog.Events;

namespace Recyclarr.Cli.Logging;

internal static class SerilogExtensions
{
    public static LogEventLevel ToLogEventLevel(this CliLogLevel level)
    {
        return level switch
        {
            CliLogLevel.Verbose => LogEventLevel.Verbose,
            CliLogLevel.Debug => LogEventLevel.Debug,
            CliLogLevel.Warn => LogEventLevel.Warning,
            _ => LogEventLevel.Information,
        };
    }
}
