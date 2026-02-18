namespace Recyclarr.Sync;

public interface ISyncRunPublisher
{
    void Publish(PipelineEvent evt);
    void Publish(SyncDiagnosticEvent evt);
}
