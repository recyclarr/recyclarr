using System.IO.Abstractions;
using Serilog;
using Serilog.Core;

namespace Recyclarr.Logging;

public class LoggerFactory
{
    private readonly IFileSystem _fs;
    private readonly LoggingLevelSwitch _logLevel;

    public LoggerFactory(IFileSystem fs, LoggingLevelSwitch logLevel)
    {
        _fs = fs;
        _logLevel = logLevel;
    }

    public ILogger Create()
    {
        var logPath = _fs.Path.Combine(AppPaths.LogDirectory, $"trash_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");

        const string consoleTemplate = "[{Level:u3}] {Message:lj}{NewLine}{Exception}";

        return new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: consoleTemplate, levelSwitch: _logLevel)
            .WriteTo.File(logPath)
            .CreateLogger();
    }
}
