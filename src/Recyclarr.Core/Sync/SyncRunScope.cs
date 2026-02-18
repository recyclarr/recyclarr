using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Recyclarr.Sync;

internal class SyncRunScope : ISyncRunScope, ISyncRunPublisher, IDisposable
{
    private readonly Subject<PipelineEvent> _pipelines = new();
    private readonly Subject<SyncDiagnosticEvent> _diagnostics = new();

    public IObservable<PipelineEvent> Pipelines => _pipelines.AsObservable();
    public IObservable<SyncDiagnosticEvent> Diagnostics => _diagnostics.AsObservable();

    public void Publish(PipelineEvent evt) => _pipelines.OnNext(evt);

    public void Publish(SyncDiagnosticEvent evt) => _diagnostics.OnNext(evt);

    public void Dispose()
    {
        _pipelines.OnCompleted();
        _diagnostics.OnCompleted();

        _pipelines.Dispose();
        _diagnostics.Dispose();
    }
}
