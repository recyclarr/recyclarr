namespace Recyclarr.Sync;

public interface ISyncRunScope
{
    IObservable<PipelineEvent> Pipelines { get; }
    IObservable<SyncDiagnosticEvent> Diagnostics { get; }
}
