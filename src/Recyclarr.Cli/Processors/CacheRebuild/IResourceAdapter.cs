using Recyclarr.Cache;
using Recyclarr.Cli.Console.Helpers;

namespace Recyclarr.Cli.Processors.CacheRebuild;

internal interface IResourceAdapter
{
    CacheableResourceType ResourceType { get; }
    string ResourceTypeName { get; }

    Task<IReadOnlyList<IServiceResource>> FetchServiceResourcesAsync(CancellationToken ct);
    IReadOnlyList<IGuideResource> GetConfiguredGuideResources();

    Dictionary<string, TrashIdMapping> LoadExistingMappings();
    void SaveMappings(List<TrashIdMapping> mappings);
    string GetCacheFilePath();
}
