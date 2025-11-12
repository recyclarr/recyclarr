using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.TrashGuide.QualitySize;

public interface IQualitySizeResourceQuery
{
    IReadOnlyList<QualitySizeResource> GetQualitySizeData(SupportedServices serviceType);
}
