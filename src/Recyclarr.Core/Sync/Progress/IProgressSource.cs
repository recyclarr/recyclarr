namespace Recyclarr.Sync.Progress;

public interface IProgressSource : IObservable<ProgressSnapshot>
{
    ProgressSnapshot Current { get; }
    void AddInstance(string name);
    void SetInstanceStatus(InstanceProgressStatus status);
    void SetPipelineStatus(PipelineProgressStatus status, int? count = null);
    void Clear();
}
