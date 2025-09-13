namespace Recyclarr.TrashGuide.QualitySize;

public class QualitySizeResourceQuery(
    IReadOnlyCollection<IQualitySizeResourceProvider> providers,
    QualitySizeGuideParser parser
) : IQualitySizeResourceQuery
{
    private readonly Lazy<
        IReadOnlyDictionary<SupportedServices, IReadOnlyList<QualitySizeData>>
    > _cache = new(() =>
    {
        var result = new Dictionary<SupportedServices, IReadOnlyList<QualitySizeData>>();

        foreach (var serviceType in Enum.GetValues<SupportedServices>())
        {
            // Get quality size directories from all providers for this service
            var qualitySizePaths = providers.SelectMany(provider =>
                provider.GetQualitySizePaths(serviceType)
            );

            result[serviceType] = parser
                .GetQualities(qualitySizePaths)
                .DistinctBy(q => q.Type) // First occurrence wins precedence
                .ToList();
        }

        return result;
    });

    public IReadOnlyList<QualitySizeData> GetQualitySizeData(SupportedServices serviceType) =>
        _cache.Value[serviceType];
}
