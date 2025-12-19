using System.Reactive.Subjects;

namespace Recyclarr.Sync;

internal sealed class SyncContextSource : ISyncContextSource, IDisposable
{
    private readonly BehaviorSubject<SyncContext> _subject = new(new SyncContext(null, null));

    public SyncContext Current => _subject.Value;

    public void SetInstance(string? instanceName)
    {
        _subject.OnNext(new SyncContext(InstanceName: instanceName, Pipeline: null));
    }

    public void SetPipeline(PipelineType? pipeline)
    {
        _subject.OnNext(_subject.Value with { Pipeline = pipeline });
    }

    public IDisposable Subscribe(IObserver<SyncContext> observer)
    {
        return _subject.Subscribe(observer);
    }

    public void Dispose()
    {
        _subject.Dispose();
    }
}
