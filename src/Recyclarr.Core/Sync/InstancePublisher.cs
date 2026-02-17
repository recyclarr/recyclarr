using Recyclarr.Config.Models;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Sync;

internal class InstancePublisher(IServiceConfiguration config, ISyncRunPublisher publisher)
    : IInstancePublisher
{
    public string Name => config.InstanceName;

    public void SetStatus(InstanceProgressStatus status)
    {
        publisher.Publish(new InstanceEvent(config.InstanceName, status));
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
