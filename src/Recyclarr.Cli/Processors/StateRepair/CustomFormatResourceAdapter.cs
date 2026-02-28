using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Pipelines.CustomFormat;
using Recyclarr.Cli.Pipelines.CustomFormat.State;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Servarr.CustomFormat;
using Recyclarr.Sync;
using Recyclarr.SyncState;

namespace Recyclarr.Cli.Processors.StateRepair;

internal class CustomFormatResourceAdapter(
    ICustomFormatService customFormatApi,
    ICustomFormatStatePersister statePersister,
    CustomFormatResourceQuery cfQuery,
    ConfiguredCustomFormatProvider cfProvider,
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
            .GetAll(IPipelinePublisher.Noop)
            .Select(entry => entry.TrashId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var allGuideCfs = cfQuery.Get(config.ServiceType);
        return allGuideCfs.Where(cf => configuredTrashIds.Contains(cf.TrashId)).ToList();
    }

    public IMappingStoreView LoadExistingMappings() => statePersister.Load();

    public void SaveMappings(List<TrashIdMapping> mappings)
    {
        var store = new TrashIdMappingStore(mappings);
        statePersister.Save(store);
    }

    public string GetStateFilePath()
    {
        return statePersister.StateFilePath;
    }
}
