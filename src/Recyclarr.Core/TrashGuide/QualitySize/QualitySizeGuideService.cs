using Recyclarr.Repo;

namespace Recyclarr.TrashGuide.QualitySize;

public class QualitySizeGuideService(
    IRepoMetadataBuilder metadataBuilder,
    QualitySizeGuideParser parser
) : IQualitySizeGuideService
{
    private readonly Dictionary<SupportedServices, IReadOnlyList<QualitySizeData>> _cache = new();

    private QualitySizePaths CreatePaths(SupportedServices serviceType)
    {
        var metadata = metadataBuilder.GetMetadata();
        return serviceType switch
        {
            SupportedServices.Radarr => new QualitySizePaths(
                metadataBuilder.ToDirectoryInfoList(metadata.JsonPaths.Radarr.Qualities)
            ),
            SupportedServices.Sonarr => new QualitySizePaths(
                metadataBuilder.ToDirectoryInfoList(metadata.JsonPaths.Sonarr.Qualities)
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, null),
        };
    }

    public IReadOnlyList<QualitySizeData> GetQualitySizeData(SupportedServices serviceType)
    {
        if (_cache.TryGetValue(serviceType, out var cfData))
        {
            return cfData;
        }

        var paths = CreatePaths(serviceType);
        return _cache[serviceType] = parser.GetQualities(paths.QualitySizeDirectories);
    }
}
