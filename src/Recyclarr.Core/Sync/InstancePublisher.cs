using Recyclarr.Config.Models;

namespace Recyclarr.Sync;

internal class InstancePublisher(IServiceConfiguration config, ISyncRunPublisher publisher)
    : IInstancePublisher
{
    public void Add(SyncOutcome outcome)
    {
        var retainOutcome = true;
        foreach (var message in SyncOutcomeFormatter.Format(outcome))
        {
            publisher.Publish(
                new SyncDiagnosticEvent(
                    config.InstanceName,
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
        publisher.Publish(
            new SyncDiagnosticEvent(config.InstanceName, SyncDiagnosticLevel.Error, message)
        );
    }

    public void AddWarning(string message)
    {
        publisher.Publish(
            new SyncDiagnosticEvent(config.InstanceName, SyncDiagnosticLevel.Warning, message)
        );
    }

    public void AddDeprecation(string message)
    {
        publisher.Publish(
            new SyncDiagnosticEvent(config.InstanceName, SyncDiagnosticLevel.Deprecation, message)
        );
    }

    public IPipelinePublisher ForPipeline(PipelineType type)
    {
        return new PipelinePublisher(config.InstanceName, type, publisher);
    }
}
