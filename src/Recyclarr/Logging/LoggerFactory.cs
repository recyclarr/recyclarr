using System.IO.Abstractions;
using Serilog;
using Serilog.Core;
using TrashLib;

namespace Recyclarr.Logging;

public class LoggerFactory
{
    private readonly IFileSystem _fs;
    private readonly IAppPaths _paths;
    private readonly LoggingLevelSwitch _logLevel;

    public LoggerFactory(IFileSystem fs, IAppPaths paths, LoggingLevelSwitch logLevel)
    {
        _fs = fs;
        _paths = paths;
        _logLevel = logLevel;
    }

    public ILogger Create()
    {
        var logPath = _fs.Path.Combine(_paths.LogDirectory, $"trash_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");

        const string consoleTemplate = "[{Level:u3}] {Message:lj}{NewLine}{Exception}";

        return new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: consoleTemplate, levelSwitch: _logLevel)
            .WriteTo.File(logPath)
            .CreateLogger();
    }
}
