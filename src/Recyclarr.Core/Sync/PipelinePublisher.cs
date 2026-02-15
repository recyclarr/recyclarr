using Recyclarr.Sync.Progress;

namespace Recyclarr.Sync;

public class PipelinePublisher(string instance, PipelineType pipeline, ISyncRunPublisher publisher)
{
    public void SetStatus(PipelineProgressStatus status, int? count = null)
    {
        publisher.Publish(new PipelineEvent(instance, pipeline, status, count));
    }

    public void AddError(string message)
    {
        publisher.Publish(new SyncDiagnosticEvent(instance, SyncDiagnosticLevel.Error, message));
    }

    public void AddWarning(string message)
    {
        publisher.Publish(new SyncDiagnosticEvent(instance, SyncDiagnosticLevel.Warning, message));
    }

    public void AddDeprecation(string message)
    {
        publisher.Publish(
            new SyncDiagnosticEvent(instance, SyncDiagnosticLevel.Deprecation, message)
        );
    }
}
