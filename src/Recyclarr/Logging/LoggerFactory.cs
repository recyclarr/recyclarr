using System.IO.Abstractions;
using Serilog;
using Serilog.Events;
using TrashLib;

namespace Recyclarr.Logging;

public class LoggerFactory
{
    private readonly IAppPaths _paths;

    private const string ConsoleTemplate = "[{Level:u3}] {Message:lj}{NewLine}{Exception}";

    public LoggerFactory(IAppPaths paths)
    {
        _paths = paths;
    }

    public ILogger Create(LogEventLevel level)
    {
        var logPath = _paths.LogDirectory.File($"trash_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");

        return new LoggerConfiguration()
            .MinimumLevel.Is(level)
            .WriteTo.Console(outputTemplate: ConsoleTemplate)
            .WriteTo.File(logPath.FullName)
            .CreateLogger();
    }
}
