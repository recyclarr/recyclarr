using Recyclarr.Cache;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Pipelines.CustomFormat;
using Recyclarr.Cli.Pipelines.CustomFormat.Cache;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ServarrApi.CustomFormat;

namespace Recyclarr.Cli.Processors.CacheRebuild;

internal class CustomFormatResourceAdapter(
    ICustomFormatApiService customFormatApi,
    ICachePersister<CustomFormatCacheObject> cachePersister,
    ICacheStoragePath cacheStoragePath,
    ConfiguredCustomFormatProvider cfProvider,
    CustomFormatResourceQuery cfQuery,
    IServiceConfiguration config
) : IResourceAdapter
{
    public CacheableResourceType ResourceType => CacheableResourceType.CustomFormats;
    public string ResourceTypeName => "custom formats";

    public async Task<IReadOnlyList<IServiceResource>> FetchServiceResourcesAsync(
        CancellationToken ct
    )
    {
        var serviceCfs = await customFormatApi.GetCustomFormats(ct);
        return serviceCfs.Cast<IServiceResource>().ToList();
    }

    public IReadOnlyList<IGuideResource> GetConfiguredGuideResources()
    {
        var configuredTrashIds = cfProvider
            .GetAll()
            .SelectMany(cfg => cfg.TrashIds)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var allGuideCfs = cfQuery.Get(config.ServiceType);
        return allGuideCfs.Where(cf => configuredTrashIds.Contains(cf.TrashId)).ToList();
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
        var cacheObject = new CustomFormatCacheObject { Mappings = mappings };
        var cache = new TrashIdCache<CustomFormatCacheObject>(cacheObject);
        cachePersister.Save(cache);
    }

    public string GetCacheFilePath()
    {
        return cacheStoragePath.CalculatePath<CustomFormatCacheObject>().FullName;
    }
}
