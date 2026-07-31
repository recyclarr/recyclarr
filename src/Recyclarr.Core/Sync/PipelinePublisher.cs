using Recyclarr.Sync.Progress;

namespace Recyclarr.Sync;

internal class PipelinePublisher(
    string instance,
    PipelineType pipeline,
    ISyncRunPublisher publisher
) : IPipelinePublisher
{
    public void SetStatus(
        PipelineProgressStatus status,
        int? count = null,
        PipelineItemChanges? changes = null
    )
    {
        publisher.Publish(new PipelineEvent(instance, pipeline, status, count, changes));
    }

    public void Add(SyncOutcome outcome)
    {
        var retainOutcome = true;
        foreach (var message in SyncOutcomeFormatter.Format(outcome))
        {
            publisher.Publish(
                new SyncDiagnosticEvent(
                    instance,
                    outcome.Level,
                    message,
                    retainOutcome ? outcome : null
                )
            );
            retainOutcome = false;
        }
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
