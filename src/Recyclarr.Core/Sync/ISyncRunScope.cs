namespace Recyclarr.Sync;

public interface ISyncRunScope
{
    IObservable<InstanceEvent> Instances { get; }
    IObservable<PipelineEvent> Pipelines { get; }
    IObservable<SyncDiagnosticEvent> Diagnostics { get; }
}
