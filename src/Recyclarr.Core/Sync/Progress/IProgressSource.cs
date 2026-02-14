namespace Recyclarr.Sync.Progress;

public interface IProgressSource
{
    IObservable<ProgressSnapshot> Observable { get; }
    void AddInstance(string name);
    void SetInstanceStatus(string instanceName, InstanceProgressStatus status);
    PipelineProgressWriter ForPipeline(string instanceName, PipelineType pipeline);
}
