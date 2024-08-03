using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases.Limits;

public class RadarrQualityItemLimits : IQualityItemLimits
{
    public decimal MaxLimit => 400m;
    public decimal PreferredLimit => 399m;
}
