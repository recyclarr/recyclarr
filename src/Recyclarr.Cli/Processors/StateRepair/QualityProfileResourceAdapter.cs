using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Pipelines.QualityProfile.State;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ServarrApi.QualityProfile;
using Recyclarr.SyncState;

namespace Recyclarr.Cli.Processors.StateRepair;

internal class QualityProfileResourceAdapter(
    IQualityProfileApiService qpApi,
    ISyncStatePersister<QualityProfileMappings> statePersister,
    ISyncStateStoragePath stateStoragePath,
    QualityProfileResourceQuery qpQuery,
    IServiceConfiguration config
) : IResourceAdapter
{
    public StatefulResourceType ResourceType => StatefulResourceType.QualityProfiles;
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

        // Use effective name: config name takes precedence over guide name,
        // matching the sync pipeline's name resolution logic.
        return config
            .QualityProfiles.Where(qp => !string.IsNullOrEmpty(qp.TrashId))
            .Where(qp => guideQps.ContainsKey(qp.TrashId!))
            .Select(qp =>
            {
                var guide = guideQps[qp.TrashId!];
                var effectiveName = string.IsNullOrEmpty(qp.Name) ? guide.Name : qp.Name;
                return (IGuideResource)(guide with { Name = effectiveName });
            })
            .Distinct()
            .ToList();
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
        var stateObject = new QualityProfileMappings { Mappings = mappings };
        var state = new TrashIdMappingStore<QualityProfileMappings>(stateObject);
        statePersister.Save(state);
    }

    public string GetStateFilePath()
    {
        return stateStoragePath.CalculatePath<QualityProfileMappings>().FullName;
    }
}
