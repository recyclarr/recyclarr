using JetBrains.Annotations;
using Recyclarr.Common.FluentValidation;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

[UsedImplicitly]
public class QualityProfileNoticePhase(ILogger log)
{
    public void Execute(QualityProfileTransactionData transactions)
    {
        if (transactions.NonExistentProfiles.Count > 0)
        {
            log.Warning(
                "The following quality profile names have no definition in the top-level `quality_profiles` " +
                "list *and* do not exist in the remote service. Either create them manually in the service *or* add " +
                "them to the top-level `quality_profiles` section so that Recyclarr can create the profiles for you");
            log.Warning("{QualityProfileNames}", transactions.NonExistentProfiles);
        }

        if (transactions.InvalidProfiles.Count > 0)
        {
            log.Warning(
                "The following validation errors occurred for one or more quality profiles. " +
                "These profiles will *not* be synced");

            var numErrors = 0;

            foreach (var (profile, errors) in transactions.InvalidProfiles)
            {
                numErrors += errors.LogValidationErrors(log, $"Profile '{profile.ProfileName}'");
            }

            if (numErrors > 0)
            {
                log.Error("Profile validation failed with {Count} errors", numErrors);
            }
        }

        var invalidQualityNames = transactions.UpdatedProfiles
            .Select(x => (x.ProfileName, x.UpdatedQualities.InvalidQualityNames))
            .Where(x => x.InvalidQualityNames.Count != 0)
            .ToList();

        foreach (var (profileName, invalidNames) in invalidQualityNames)
        {
            log.Warning("Quality profile '{ProfileName}' references invalid quality names: {InvalidNames}",
                profileName, invalidNames);
        }

        var invalidCfExceptNames = transactions.UpdatedProfiles
            .Where(x => x.InvalidExceptCfNames.Count != 0)
            .Select(x => (x.ProfileName, x.InvalidExceptCfNames));

        foreach (var (profileName, invalidNames) in invalidCfExceptNames)
        {
            log.Warning(
                "`except` under `reset_unmatched_scores` in quality profile '{ProfileName}' has invalid " +
                "CF names: {CfNames}", profileName, invalidNames);
        }
    }
}
