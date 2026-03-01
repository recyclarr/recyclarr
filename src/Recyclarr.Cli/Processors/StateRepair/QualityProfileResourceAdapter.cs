using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Pipelines.QualityProfile.State;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Servarr.QualityProfile;
using Recyclarr.SyncState;

namespace Recyclarr.Cli.Processors.StateRepair;

internal class QualityProfileResourceAdapter(
    IQualityProfileService qpService,
    IQualityProfileStatePersister statePersister,
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
        var serviceQps = await qpService.GetQualityProfiles(ct);
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
