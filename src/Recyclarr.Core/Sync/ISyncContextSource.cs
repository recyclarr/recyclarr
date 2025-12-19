namespace Recyclarr.Sync;

public interface ISyncContextSource : IObservable<SyncContext>
{
    SyncContext Current { get; }
    void SetInstance(string? instanceName);
    void SetPipeline(PipelineType? pipeline);
}
