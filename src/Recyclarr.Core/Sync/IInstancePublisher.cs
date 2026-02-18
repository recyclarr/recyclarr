namespace Recyclarr.Sync;

public interface IInstancePublisher : IDiagnosticPublisher
{
    IPipelinePublisher ForPipeline(PipelineType type);
}
