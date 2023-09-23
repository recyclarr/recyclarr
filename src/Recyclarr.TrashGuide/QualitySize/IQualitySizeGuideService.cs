using Recyclarr.Common;

namespace Recyclarr.TrashGuide.QualitySize;

public interface IQualitySizeGuideService
{
    IReadOnlyList<QualitySizeData> GetQualitySizeData(SupportedServices serviceType);
}
