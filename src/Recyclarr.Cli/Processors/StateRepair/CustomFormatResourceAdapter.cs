using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Pipelines.CustomFormat;
using Recyclarr.Cli.Pipelines.CustomFormat.State;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ServarrApi.CustomFormat;
using Recyclarr.SyncState;

namespace Recyclarr.Cli.Processors.StateRepair;

internal class CustomFormatResourceAdapter(
    ICustomFormatApiService customFormatApi,
    ISyncStatePersister<CustomFormatMappings> statePersister,
    ISyncStateStoragePath stateStoragePath,
    ConfiguredCustomFormatProvider cfProvider,
    CustomFormatResourceQuery cfQuery,
    IServiceConfiguration config
) : IResourceAdapter
{
    public StatefulResourceType ResourceType => StatefulResourceType.CustomFormats;
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
            .Select(entry => entry.TrashId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var allGuideCfs = cfQuery.Get(config.ServiceType);
        return allGuideCfs.Where(cf => configuredTrashIds.Contains(cf.TrashId)).ToList();
    }

    public Dictionary<string, TrashIdMapping> LoadExistingMappings()
    {
        var existingState = statePersister.Load();
        return existingState.Mappings.ToDictionary(
            m => m.TrashId,
            StringComparer.OrdinalIgnoreCase
        );
    }

    public void SaveMappings(List<TrashIdMapping> mappings)
    {
        var stateObject = new CustomFormatMappings { Mappings = mappings };
        var state = new TrashIdMappingStore<CustomFormatMappings>(stateObject);
        statePersister.Save(state);
    }

    public string GetStateFilePath()
    {
        return stateStoragePath.CalculatePath<CustomFormatMappings>().FullName;
    }
}
