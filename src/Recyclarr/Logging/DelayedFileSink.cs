using System.IO.Abstractions;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using TrashLib;

namespace Recyclarr.Logging;

public sealed class DelayedFileSink : IDelayedFileSink
{
    private readonly IAppPaths _paths;
    private readonly Lazy<StreamWriter> _stream;
    private ITextFormatter? _formatter;

    public DelayedFileSink(IAppPaths paths, IFileSystem fs)
    {
        _paths = paths;
        _stream = new Lazy<StreamWriter>(() =>
        {
            var logPath = fs.Path.Combine(_paths.LogDirectory, $"trash_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");
            return fs.File.CreateText(logPath);
        });
    }

    public void Emit(LogEvent logEvent)
    {
        if (!_paths.IsAppDataPathValid)
        {
            return;
        }

        _formatter?.Format(logEvent, _stream.Value);
        _stream.Value.Flush();
    }

    public void Dispose()
    {
        if (_stream.IsValueCreated)
        {
            _stream.Value.Close();
        }
    }

    public void SetTemplate(string template)
    {
        _formatter = new MessageTemplateTextFormatter(template);
    }
}
