using Recyclarr.Cli.Pipelines.ReleaseProfile.Models;
using Recyclarr.Cli.Pipelines.Tags;
using Recyclarr.Common.Extensions;
using Recyclarr.ServarrApi.ReleaseProfile;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile.PipelinePhases;

public class ReleaseProfileTransactionPhase
{
    private readonly ServiceTagCache _tagCache;

    public ReleaseProfileTransactionPhase(ServiceTagCache tagCache)
    {
        _tagCache = tagCache;
    }

    public ReleaseProfileTransactionData Execute(
        IReadOnlyList<ProcessedReleaseProfileData> configProfiles,
        IList<SonarrReleaseProfile> serviceData)
    {
        var created = new List<SonarrReleaseProfile>();
        var updated = new List<SonarrReleaseProfile>();

        foreach (var configProfile in configProfiles)
        {
            var title = $"[Trash] {configProfile.Profile.Name}";
            var matchingServiceProfile = serviceData.FirstOrDefault(x => x.Name.EqualsIgnoreCase(title));
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

        var deleted = DeleteOldManagedProfiles(serviceData, configProfiles);

        return new ReleaseProfileTransactionData(updated, created, deleted);
    }

    private static IReadOnlyList<SonarrReleaseProfile> DeleteOldManagedProfiles(
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
            .Select(x => _tagCache.GetTagIdByName(x))
            .NotNull()
            .ToList();
    }
}
