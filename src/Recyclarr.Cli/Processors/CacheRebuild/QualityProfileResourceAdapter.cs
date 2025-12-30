using Recyclarr.Cache;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Pipelines.QualityProfile.Cache;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Processors.CacheRebuild;

internal class QualityProfileResourceAdapter(
    IQualityProfileApiService qpApi,
    ICachePersister<QualityProfileCacheObject> cachePersister,
    ICacheStoragePath cacheStoragePath,
    QualityProfileResourceQuery qpQuery,
    IServiceConfiguration config
) : IResourceAdapter
{
    public CacheableResourceType ResourceType => CacheableResourceType.QualityProfiles;
    public string ResourceTypeName => "quality profiles";

    public async Task<IReadOnlyList<IServiceResource>> FetchServiceResourcesAsync(
        CancellationToken ct
    )
    {
        var serviceQps = await qpApi.GetQualityProfiles(ct);
        return serviceQps.Cast<IServiceResource>().ToList();
    }

    public IReadOnlyList<IGuideResource> GetConfiguredGuideResources()
    {
        var guideQps = qpQuery
            .Get(config.ServiceType)
            .ToDictionary(qp => qp.TrashId, StringComparer.OrdinalIgnoreCase);

        // Get configured QPs that have a trash_id and exist in the guide
        return config
            .QualityProfiles.Where(qp => !string.IsNullOrEmpty(qp.TrashId))
            .Where(qp => guideQps.ContainsKey(qp.TrashId!))
            .Select(qp => guideQps[qp.TrashId!])
            .Distinct()
            .Cast<IGuideResource>()
            .ToList();
    }

    public Dictionary<string, TrashIdMapping> LoadExistingMappings()
    {
        var existingCache = cachePersister.Load();
        return existingCache.Mappings.ToDictionary(
            m => m.TrashId,
            StringComparer.OrdinalIgnoreCase
        );
    }

    public void SaveMappings(List<TrashIdMapping> mappings)
    {
        var cacheObject = new QualityProfileCacheObject { Mappings = mappings };
        var cache = new TrashIdCache<QualityProfileCacheObject>(cacheObject);
        cachePersister.Save(cache);
    }

    public string GetCacheFilePath()
    {
        return cacheStoragePath.CalculatePath<QualityProfileCacheObject>().FullName;
    }
}
