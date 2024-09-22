using System.IO.Abstractions;
using Recyclarr.Logging;
using Recyclarr.Platform;
using Serilog.Events;
using Serilog.Templates;

namespace Recyclarr.Cli.Logging;

internal class FileLogSinkConfigurator(IAppPaths paths) : ILogConfigurator
{
    public void Configure(LoggerConfiguration config)
    {
        var logFilePrefix = $"recyclarr_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
        var logDir = paths.LogDirectory;
        var template = BuildExpressionTemplate();

        config
            .WriteTo.Logger(c => c
                .MinimumLevel.Debug()
                .WriteTo.File(template, LogFilePath("debug")))
            .WriteTo.Logger(c => c
                .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Verbose)
                .WriteTo.File(template, LogFilePath("verbose")));

        return;

        string LogFilePath(string type)
        {
            return logDir.File($"{logFilePrefix}.{type}.log").FullName;
        }
    }

    private static ExpressionTemplate BuildExpressionTemplate()
    {
        var template = "[{@t:HH:mm:ss} {@l:u3}] " + LogTemplates.Base +
            "{Inspect(@x).StackTrace}";

        return new ExpressionTemplate(template);
    }
}
