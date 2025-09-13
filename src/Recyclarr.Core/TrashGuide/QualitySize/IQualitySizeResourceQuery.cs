namespace Recyclarr.TrashGuide.QualitySize;

public interface IQualitySizeResourceQuery
{
    IReadOnlyList<QualitySizeData> GetQualitySizeData(SupportedServices serviceType);
}
