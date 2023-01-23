using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Repo;

namespace Recyclarr.TrashLib.Pipelines.QualitySize.Guide;

public class QualityGuideService : IQualityGuideService
{
    private readonly IRepoMetadataBuilder _metadataBuilder;
    private readonly QualitySizeGuideParser _parser;
    private readonly Dictionary<SupportedServices, IReadOnlyList<QualitySizeData>> _cache = new();

    public QualityGuideService(
        IRepoMetadataBuilder metadataBuilder,
        QualitySizeGuideParser parser)
    {
        _metadataBuilder = metadataBuilder;
        _parser = parser;
    }

    private QualitySizePaths CreatePaths(SupportedServices serviceType)
    {
        var metadata = _metadataBuilder.GetMetadata();
        return serviceType switch
        {
            SupportedServices.Radarr => new QualitySizePaths(
                _metadataBuilder.ToDirectoryInfoList(metadata.JsonPaths.Radarr.Qualities)
            ),
            SupportedServices.Sonarr => new QualitySizePaths(
                _metadataBuilder.ToDirectoryInfoList(metadata.JsonPaths.Sonarr.Qualities)
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, null)
        };
    }

    public IReadOnlyList<QualitySizeData> GetQualitySizeData(SupportedServices serviceType)
    {
        if (_cache.TryGetValue(serviceType, out var cfData))
        {
            return cfData;
        }

        var paths = CreatePaths(serviceType);
        return _cache[serviceType] = _parser.GetQualities(paths.QualitySizeDirectories);
    }
}
