namespace Recyclarr.Pipelines.Plan.Components;

/// <summary>
/// Used where quality sizes are global (Sonarr v4, Radarr), so profiles receive no sizes. A
/// profile's YAML <c>quality_definition</c> is reported as ignored; guide references are ignored
/// silently because users cannot change guide data (PDR-008).
/// </summary>
internal class GlobalQualitySizeProfilePlanner : IQualityProfileSizePlanner
{
    public IReadOnlyCollection<PlannedQualityProfile> Plan(
        IReadOnlyCollection<PlannedQualityProfile> profiles,
        PipelinePlan plan
    )
    {
        foreach (
            var profile in profiles.Where(x =>
                x.IsManaged && x.Config.QualityDefinition is not null
            )
        )
        {
            plan.Add(new ProfileQualityDefinitionIgnoredOutcome(profile));
        }

        return profiles;
    }
}
