using Recyclarr.Sync.Progress;

namespace Recyclarr.Sync;

public interface IInstancePublisher : IDiagnosticPublisher
{
    string Name { get; }
    void SetStatus(InstanceProgressStatus status);
    IPipelinePublisher ForPipeline(PipelineType type);
}
