namespace Recyclarr.Sync.Events;

public class SyncEventCollector(ILogger log, SyncEventStorage storage) : ISyncEventCollector
{
    private string? _currentInstance;
    private PipelineType? _currentPipeline;

    public void SetInstance(string? instanceName) => _currentInstance = instanceName;

    public void SetPipeline(PipelineType? pipeline) => _currentPipeline = pipeline;

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
}
