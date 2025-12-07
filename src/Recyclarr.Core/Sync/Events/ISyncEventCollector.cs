namespace Recyclarr.Sync.Events;

public interface ISyncEventCollector
{
    void SetInstance(string? instanceName);
    void SetPipeline(PipelineType? pipeline);
    void AddError(string message);
    void AddWarning(string message);
    void AddDeprecation(string message);
    void AddCompletionCount(int count);
}
