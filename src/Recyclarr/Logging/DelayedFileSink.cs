using System.IO.Abstractions;
using Serilog.Events;
using TrashLib;

namespace Recyclarr.Logging;

public class DelayedFileSink : IDelayedFileSink
{
    private readonly IAppPaths _paths;
    private readonly Lazy<StreamWriter> _stream;

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

        _stream.Value.WriteLine(logEvent.RenderMessage());
    }

    public void Dispose()
    {
        if (_stream.IsValueCreated)
        {
            _stream.Value.Close();
        }
    }
}
