using Recyclarr.TrashLib.Config;

namespace Recyclarr.TrashLib.Pipelines.QualitySize.Guide;

public interface IQualityGuideService
{
    IReadOnlyList<QualitySizeData> GetQualitySizeData(SupportedServices serviceType);
}
