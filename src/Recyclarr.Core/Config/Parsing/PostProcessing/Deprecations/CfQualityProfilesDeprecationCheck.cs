namespace Recyclarr.Config.Parsing.PostProcessing.Deprecations;

public class CfQualityProfilesDeprecationCheck(ILogger log) : IConfigDeprecationCheck
{
    public bool CheckIfNeeded(ServiceConfigYaml include)
    {
        return
            include.CustomFormats is not null &&
            include.CustomFormats.Any(x => x.QualityProfiles is {Count: > 0});
    }

    public ServiceConfigYaml Transform(ServiceConfigYaml include)
    {
        log.Warning(
            "DEPRECATED: The `quality_profiles` element under `custom_formats` nodes was " +
            "detected in your config. This has been renamed to `assign_scores_to`. " +
            "See: <https://recyclarr.dev/wiki/upgrade-guide/v8.0/#assign-scores-to>");

        // CustomFormats is checked for null in the CheckIfNeeded() method, which is called first.
        var cfs = include.CustomFormats!.Select(x => x with
        {
            AssignScoresTo = [..x.AssignScoresTo ?? [], ..x.QualityProfiles ?? []],
            QualityProfiles = null
        });

        return include with
        {
            CustomFormats = cfs.ToList()
        };
    }
}
