using Recyclarr.TrashLib.Config;

namespace Recyclarr.Cli.Pipelines.QualitySize.Guide;

public interface IQualityGuideService
{
    IReadOnlyList<QualitySizeData> GetQualitySizeData(SupportedServices serviceType);
}
