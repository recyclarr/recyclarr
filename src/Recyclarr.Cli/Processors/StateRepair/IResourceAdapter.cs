using Recyclarr.Cli.Console.Helpers;
using Recyclarr.SyncState;

namespace Recyclarr.Cli.Processors.StateRepair;

internal interface IResourceAdapter
{
    StatefulResourceType ResourceType { get; }
    string ResourceTypeName { get; }

    Task<IReadOnlyList<IServiceResource>> FetchServiceResourcesAsync(CancellationToken ct);
    IReadOnlyList<IGuideResource> GetConfiguredGuideResources();

    Dictionary<string, TrashIdMapping> LoadExistingMappings();
    void SaveMappings(List<TrashIdMapping> mappings);
    string GetStateFilePath();
}
