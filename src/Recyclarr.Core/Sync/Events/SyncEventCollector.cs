namespace Recyclarr.Sync.Events;

public class SyncEventCollector(ILogger log, SyncEventStorage storage)
    : ISyncScopeFactory,
        ISyncEventPublisher
{
    private string? _currentInstance;
    private PipelineType? _currentPipeline;

    public IDisposable SetInstance(string instanceName)
    {
        _currentInstance = instanceName;
        return new ContextScope(() => _currentInstance = null);
    }

    public IDisposable SetPipeline(PipelineType pipeline)
    {
        _currentPipeline = pipeline;
        return new ContextScope(() => _currentPipeline = null);
    }

    public void AddError(string message, Exception? exception = null)
    {
        log.Error(exception, "{Message}", message);
        storage.Add(
            new DiagnosticEvent(_currentInstance, _currentPipeline, DiagnosticType.Error, message)
        );
    }

    public void AddWarning(string message)
    {
        log.Warning("{Message}", message);
        storage.Add(
            new DiagnosticEvent(_currentInstance, _currentPipeline, DiagnosticType.Warning, message)
        );
    }

    public void AddDeprecation(string message)
    {
        log.Warning("{Message}", message);
        storage.Add(
            new DiagnosticEvent(
                _currentInstance,
                _currentPipeline,
                DiagnosticType.Deprecation,
                message
            )
        );
    }

    public void AddCompletionCount(int count) =>
        storage.Add(new CompletionEvent(_currentInstance, _currentPipeline, count));

    private sealed class ContextScope(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
