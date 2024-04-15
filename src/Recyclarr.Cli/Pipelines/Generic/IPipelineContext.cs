using Recyclarr.TrashGuide;

namespace Recyclarr.Cli.Pipelines.Generic;

public interface IPipelineContext
{
    string PipelineDescription { get; }
    IReadOnlyCollection<SupportedServices> SupportedServiceTypes { get; }
}
