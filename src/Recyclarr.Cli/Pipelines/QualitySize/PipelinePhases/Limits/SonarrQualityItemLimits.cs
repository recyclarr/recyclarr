using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases.Limits;

public class SonarrQualityItemLimits : IQualityItemLimits
{
    public decimal MaxLimit => 400m;
    public decimal PreferredLimit => 395m;
}
