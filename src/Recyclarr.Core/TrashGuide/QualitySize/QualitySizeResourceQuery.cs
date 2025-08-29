namespace Recyclarr.TrashGuide.QualitySize;

public class QualitySizeResourceQuery(
    IEnumerable<IQualitySizeResourceProvider> providers,
    QualitySizeGuideParser parser
) : IQualitySizeResourceQuery
{
    private readonly Dictionary<SupportedServices, IReadOnlyList<QualitySizeData>> _cache = new();

    public IReadOnlyList<QualitySizeData> GetQualitySizeData(SupportedServices serviceType)
    {
        if (_cache.TryGetValue(serviceType, out var cfData))
        {
            return cfData;
        }

        // Get quality size directories from all providers
        var qualitySizePaths = providers.SelectMany(provider =>
            provider.GetQualitySizePaths(serviceType)
        );

        return _cache[serviceType] = parser.GetQualities(qualitySizePaths);
    }
}
