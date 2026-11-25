namespace Recyclarr.Pipelines.Plan.Components;

/// <summary>
/// Plans a profile's configured <c>language</c> for the instance's API target. Only some services'
/// profiles have a language (ADR-023), so the selected implementation decides whether the setting
/// applies or is reported as ignored. Outcomes are reported through the context.
/// </summary>
internal interface IQualityProfileLanguagePlanner
{
    void Plan(IReadOnlyCollection<PlannedQualityProfile> profiles, PlanningContext context);
}

/// <summary>
/// Used where profiles have no language (Sonarr): each profile that sets <c>language</c> is
/// reported as ignored. The profile still syncs.
/// </summary>
internal class LanguageIgnoredPlanner : IQualityProfileLanguagePlanner
{
    public void Plan(IReadOnlyCollection<PlannedQualityProfile> profiles, PlanningContext context)
    {
        foreach (var profile in profiles.Where(x => x.Config.Language is not null))
        {
            context.Add(new ProfileLanguageIgnoredOutcome(profile));
        }
    }
}

/// <summary>
/// Used where profiles have a language (Radarr): nothing to report while planning. The language is
/// applied and validated against the instance during sync.
/// </summary>
internal class LanguageSupportedPlanner : IQualityProfileLanguagePlanner
{
    public void Plan(
        IReadOnlyCollection<PlannedQualityProfile> profiles,
        PlanningContext context
    ) { }
}
