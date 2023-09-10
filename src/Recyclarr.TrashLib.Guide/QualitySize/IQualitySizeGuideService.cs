using Recyclarr.TrashLib.Config;

namespace Recyclarr.TrashLib.Guide.QualitySize;

public interface IQualitySizeGuideService
{
    IReadOnlyList<QualitySizeData> GetQualitySizeData(SupportedServices serviceType);
}
