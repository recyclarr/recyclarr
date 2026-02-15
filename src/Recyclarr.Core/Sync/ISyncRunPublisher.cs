namespace Recyclarr.Sync;

public interface ISyncRunPublisher
{
    void Publish(InstanceEvent evt);
    void Publish(PipelineEvent evt);
    void Publish(SyncDiagnosticEvent evt);
}
