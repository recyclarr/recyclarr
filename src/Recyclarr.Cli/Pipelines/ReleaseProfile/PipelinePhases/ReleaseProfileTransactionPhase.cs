using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Cli.Pipelines.ReleaseProfile.Models;
using Recyclarr.Cli.Pipelines.Tags;
using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.ReleaseProfile;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile.PipelinePhases;

public class ReleaseProfileTransactionPhase(ServiceTagCache tagCache)
    : ITransactionPipelinePhase<ReleaseProfilePipelineContext>
{
    public void Execute(ReleaseProfilePipelineContext context, IServiceConfiguration config)
    {
        var created = new List<SonarrReleaseProfile>();
        var updated = new List<SonarrReleaseProfile>();

        foreach (var configProfile in context.ConfigOutput)
        {
            var title = $"[Trash] {configProfile.Profile.Name}";
            var matchingServiceProfile = context.ApiFetchOutput.FirstOrDefault(x => x.Name.EqualsIgnoreCase(title));
            if (matchingServiceProfile is not null)
            {
                SetupProfileRequestObject(matchingServiceProfile, configProfile);
                updated.Add(matchingServiceProfile);
            }
            else
            {
                var profileToUpdate = new SonarrReleaseProfile {Name = title, Enabled = true};
                SetupProfileRequestObject(profileToUpdate, configProfile);
                created.Add(profileToUpdate);
            }
        }

        var deleted = DeleteOldManagedProfiles(context.ApiFetchOutput, context.ConfigOutput.AsReadOnly());
        context.TransactionOutput = new ReleaseProfileTransactionData(updated, created, deleted);
    }

    private static List<SonarrReleaseProfile> DeleteOldManagedProfiles(
        IList<SonarrReleaseProfile> serviceData,
        IReadOnlyList<ProcessedReleaseProfileData> configProfiles)
    {
        var profiles = configProfiles.Select(x => x.Profile).ToList();
        return serviceData
            .Where(sonarrProfile =>
            {
                return sonarrProfile.Name.StartsWithIgnoreCase("[Trash]") &&
                    !profiles.Exists(profile => sonarrProfile.Name.EndsWithIgnoreCase(profile.Name));
            })
            .ToList();
    }

    private void SetupProfileRequestObject(SonarrReleaseProfile profileToUpdate, ProcessedReleaseProfileData profile)
    {
        profileToUpdate.Preferred = profile.Profile.Preferred
            .SelectMany(x => x.Terms.Select(termData => new SonarrPreferredTerm(x.Score, termData.Term)))
            .ToList();

        profileToUpdate.Ignored = profile.Profile.Ignored.Select(x => x.Term).ToList();
        profileToUpdate.Required = profile.Profile.Required.Select(x => x.Term).ToList();
        profileToUpdate.IncludePreferredWhenRenaming = profile.Profile.IncludePreferredWhenRenaming;
        profileToUpdate.Tags = profile.Tags
            .Select(tagCache.GetTagIdByName)
            .NotNull()
            .ToList();
    }
}
