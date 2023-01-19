using Recyclarr.TrashLib.Config;

namespace Recyclarr.TrashLib.Services.QualitySize.Guide;

public interface IQualityGuideService
{
    IReadOnlyList<QualitySizeData> GetQualitySizeData(SupportedServices serviceType);
}
