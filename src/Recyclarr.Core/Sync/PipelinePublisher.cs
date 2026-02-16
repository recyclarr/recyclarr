using Recyclarr.Sync.Progress;

namespace Recyclarr.Sync;

public class PipelinePublisher(string instance, PipelineType pipeline, ISyncRunPublisher publisher)
{
    public static PipelinePublisher Noop { get; } = new("", default, new NoopPublisher());

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

    private sealed class NoopPublisher : ISyncRunPublisher
    {
        public void Publish(InstanceEvent evt) { }

        public void Publish(PipelineEvent evt) { }

        public void Publish(SyncDiagnosticEvent evt) { }
    }
}
