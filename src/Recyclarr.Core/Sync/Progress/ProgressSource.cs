using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Recyclarr.Sync.Progress;

internal class ProgressSource(ISyncContextSource contextSource) : IProgressSource, IDisposable
{
    private readonly BehaviorSubject<ProgressSnapshot> _subject = new(new ProgressSnapshot([]));

    public IObservable<ProgressSnapshot> Observable => _subject.AsObservable();

    public void AddInstance(string name)
    {
        // Capture current state for consistent read-modify-write (immutable update pattern)
        var snapshot = _subject.Value;
        var instance = new InstanceSnapshot(
            name,
            InstanceProgressStatus.Pending,
            ImmutableDictionary<PipelineType, PipelineSnapshot>.Empty
        );
        _subject.OnNext(new ProgressSnapshot(snapshot.Instances.Add(instance)));
    }

    public void SetInstanceStatus(InstanceProgressStatus status)
    {
        var name =
            contextSource.Current.InstanceName
            ?? throw new InvalidOperationException(
                "Instance name must be set before updating status"
            );

        var snapshot = _subject.Value;
        var index = snapshot.Instances.FindIndex(i =>
            i.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
        );
        if (index < 0)
        {
            return;
        }

        var instance = snapshot.Instances[index];
        var updated = instance with { Status = status };
        _subject.OnNext(
            new ProgressSnapshot(Instances: snapshot.Instances.SetItem(index, updated))
        );
    }

    public void SetPipelineStatus(PipelineProgressStatus status, int? count = null)
    {
        var ctx = contextSource.Current;
        if (ctx.InstanceName is null || ctx.Pipeline is null)
        {
            return;
        }

        UpdatePipelineSnapshot(ctx.InstanceName, ctx.Pipeline.Value, status, count);
    }

    private void UpdatePipelineSnapshot(
        string instanceName,
        PipelineType pipeline,
        PipelineProgressStatus status,
        int? count
    )
    {
        var snapshot = _subject.Value;
        var index = snapshot.Instances.FindIndex(i =>
            i.Name.Equals(instanceName, StringComparison.OrdinalIgnoreCase)
        );
        if (index < 0)
        {
            return;
        }

        var instance = snapshot.Instances[index];
        var pipelineSnapshot = new PipelineSnapshot(status, count);
        var updatedPipelines = instance.Pipelines.SetItem(pipeline, pipelineSnapshot);
        var updated = instance with { Pipelines = updatedPipelines };
        _subject.OnNext(
            new ProgressSnapshot(Instances: snapshot.Instances.SetItem(index, updated))
        );
    }

    public void Clear()
    {
        _subject.OnNext(new ProgressSnapshot([]));
    }

    public void Dispose()
    {
        _subject.Dispose();
    }
}
