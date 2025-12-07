namespace Recyclarr.Sync.Events;

public class SyncEventCollector(SyncEventStorage storage) : ISyncEventCollector
{
    private string? _currentInstance;
    private PipelineType? _currentPipeline;

    public void SetInstance(string? instanceName) => _currentInstance = instanceName;

    public void SetPipeline(PipelineType? pipeline) => _currentPipeline = pipeline;

    public void AddError(string message) =>
        storage.Add(
            new DiagnosticEvent(_currentInstance, _currentPipeline, DiagnosticType.Error, message)
        );

    public void AddWarning(string message) =>
        storage.Add(
            new DiagnosticEvent(_currentInstance, _currentPipeline, DiagnosticType.Warning, message)
        );

    public void AddDeprecation(string message) =>
        storage.Add(
            new DiagnosticEvent(
                _currentInstance,
                _currentPipeline,
                DiagnosticType.Deprecation,
                message
            )
        );

    public void AddCompletionCount(int count) =>
        storage.Add(new CompletionEvent(_currentInstance, _currentPipeline, count));
}
