using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Sync.Events;
using Recyclarr.TrashGuide;

namespace Recyclarr.Cli.Pipelines.Plan.Components;

internal class CustomFormatPlanComponent(
    CustomFormatResourceQuery guide,
    IServiceConfiguration config
) : IPlanComponent
{
    public void Process(PipelinePlan plan, ISyncEventPublisher events)
    {
        var customFormats = GetCustomFormatsForService();

        // Match custom formats in the YAML config to those in the guide, by Trash ID
        // Conservative approach: only CFs in the guide that are specified in the config
        var processedCfs = config
            .CustomFormats.SelectMany(x => x.TrashIds)
            .Distinct(StringComparer.InvariantCultureIgnoreCase)
            .GroupJoin(
                customFormats,
                x => x,
                x => x.TrashId,
                (id, cf) => (Id: id, CustomFormats: cf)
            )
            .ToLookup(x => x.CustomFormats.Any());

        // Track invalid trash_ids in diagnostics
        foreach (var invalid in processedCfs[false])
        {
            events.AddWarning($"Invalid trash_id: {invalid.Id}");
        }

        // Add matched CFs to plan
        foreach (var cf in processedCfs[true].SelectMany(x => x.CustomFormats))
        {
            plan.AddCustomFormat(new PlannedCustomFormat(cf));
        }
    }

    private IEnumerable<CustomFormatResource> GetCustomFormatsForService()
    {
        return config.ServiceType switch
        {
            SupportedServices.Radarr => guide.GetRadarr(),
            SupportedServices.Sonarr => guide.GetSonarr(),
            _ => throw new InvalidOperationException($"Unknown service type: {config.ServiceType}"),
        };
    }
}
