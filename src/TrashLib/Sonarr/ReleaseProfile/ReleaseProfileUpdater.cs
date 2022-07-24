using System.Reactive.Linq;
using CliFx.Infrastructure;
using Common.Extensions;
using Serilog;
using TrashLib.ExceptionTypes;
using TrashLib.Sonarr.Api;
using TrashLib.Sonarr.Api.Objects;
using TrashLib.Sonarr.Config;
using TrashLib.Sonarr.ReleaseProfile.Filters;
using TrashLib.Sonarr.ReleaseProfile.Guide;

namespace TrashLib.Sonarr.ReleaseProfile;

public class ReleaseProfileUpdater : IReleaseProfileUpdater
{
    private readonly ISonarrApi _api;
    private readonly ISonarrCompatibility _compatibility;
    private readonly IReleaseProfileFilterPipeline _pipeline;
    private readonly IConsole _console;
    private readonly ISonarrGuideService _guide;
    private readonly ILogger _log;

    public ReleaseProfileUpdater(
        ILogger logger,
        ISonarrGuideService guide,
        ISonarrApi api,
        ISonarrCompatibility compatibility,
        IReleaseProfileFilterPipeline pipeline,
        IConsole console)
    {
        _log = logger;
        _guide = guide;
        _api = api;
        _compatibility = compatibility;
        _pipeline = pipeline;
        _console = console;
    }

    public async Task Process(bool isPreview, SonarrConfiguration config)
    {
        var profilesFromGuide = _guide.GetReleaseProfileData();

        var filteredProfiles = new List<(ReleaseProfileData Profile, IReadOnlyCollection<string> Tags)>();

        var configProfiles = config.ReleaseProfiles.SelectMany(x => x.TrashIds.Select(y => (TrashId: y, Config: x)));
        foreach (var (trashId, configProfile) in configProfiles)
        {
            // For each release profile specified in our YAML config, find the matching profile in the guide.
            var selectedProfile = profilesFromGuide.FirstOrDefault(x => x.TrashId.EqualsIgnoreCase(trashId));
            if (selectedProfile is null)
            {
                _log.Warning("A release profile with Trash ID {TrashId} does not exist", trashId);
                continue;
            }

            _log.Debug("Found Release Profile: {ProfileName} ({TrashId})", selectedProfile.Name,
                selectedProfile.TrashId);

            selectedProfile = _pipeline.Process(selectedProfile, configProfile);

            if (isPreview)
            {
                PrintTermsAndScores(selectedProfile);
                continue;
            }

            filteredProfiles.Add((selectedProfile, configProfile.Tags));
        }

        await ProcessReleaseProfiles(filteredProfiles);
    }

    private void PrintTermsAndScores(ReleaseProfileData profile)
    {
        void PrintPreferredTerms(string title, IReadOnlyCollection<PreferredTermData> preferredTerms)
        {
            if (preferredTerms.Count <= 0)
            {
                return;
            }

            _console.Output.WriteLine($"  {title}:");
            foreach (var (score, terms) in preferredTerms)
            {
                foreach (var term in terms)
                {
                    _console.Output.WriteLine($"    {score,-10} {term}");
                }
            }

            _console.Output.WriteLine("");
        }

        void PrintTerms(string title, IReadOnlyCollection<TermData> terms)
        {
            if (terms.Count == 0)
            {
                return;
            }

            _console.Output.WriteLine($"  {title}:");
            foreach (var term in terms)
            {
                _console.Output.WriteLine($"    {term}");
            }

            _console.Output.WriteLine("");
        }

        _console.Output.WriteLine("");

        _console.Output.WriteLine(profile.Name);

        _console.Output.WriteLine("  Include Preferred when Renaming?");
        _console.Output.WriteLine("    " +
                                  (profile.IncludePreferredWhenRenaming ? "YES" : "NO"));
        _console.Output.WriteLine("");

        PrintTerms("Must Contain", profile.Required);
        PrintTerms("Must Not Contain", profile.Ignored);
        PrintPreferredTerms("Preferred", profile.Preferred);

        _console.Output.WriteLine("");
    }

    private async Task ProcessReleaseProfiles(
        List<(ReleaseProfileData Profile, IReadOnlyCollection<string> Tags)> profilesAndTags)
    {
        await DoVersionEnforcement();

        // Obtain all of the existing release profiles first. If any were previously created by our program
        // here, we favor replacing those instead of creating new ones, which would just be mostly duplicates
        // (but with some differences, since there have likely been updates since the last run).
        var existingProfiles = await _api.GetReleaseProfiles();

        foreach (var (profile, tags) in profilesAndTags)
        {
            // If tags were provided, ensure they exist. Tags that do not exist are added first, so that we
            // may specify them with the release profile request payload.
            var tagIds = await CreateTagsInSonarr(tags);

            var title = BuildProfileTitle(profile.Name);
            var profileToUpdate = GetProfileToUpdate(existingProfiles, title);
            if (profileToUpdate != null)
            {
                _log.Information("Update existing profile: {ProfileName}", title);
                await UpdateExistingProfile(profileToUpdate, profile, tagIds);
            }
            else
            {
                _log.Information("Create new profile: {ProfileName}", title);
                await CreateNewProfile(title, profile, tagIds);
            }
        }

        // Any profiles with `[Trash]` in front of their name are managed exclusively by Recyclarr. As such, if
        // there are any still in Sonarr that we didn't update, those are most certainly old and shouldn't be kept
        // around anymore.
        await DeleteOldManagedProfiles(profilesAndTags, existingProfiles);
    }

    private async Task DeleteOldManagedProfiles(
        IEnumerable<(ReleaseProfileData Profile, IReadOnlyCollection<string> Tags)> profilesAndTags,
        IEnumerable<SonarrReleaseProfile> sonarrProfiles)
    {
        var profiles = profilesAndTags.Select(x => x.Profile).ToList();
        var sonarrProfilesToDelete = sonarrProfiles
            .Where(sonarrProfile =>
            {
                return sonarrProfile.Name.StartsWithIgnoreCase("[Trash]") &&
                       !profiles.Any(profile => sonarrProfile.Name.EndsWithIgnoreCase(profile.Name));
            });

        foreach (var profile in sonarrProfilesToDelete)
        {
            _log.Information("Deleting old Trash release profile: {ProfileName}", profile.Name);
            await _api.DeleteReleaseProfile(profile.Id);
        }
    }

    private async Task<IReadOnlyCollection<int>> CreateTagsInSonarr(IReadOnlyCollection<string> tags)
    {
        if (!tags.Any())
        {
            return Array.Empty<int>();
        }

        var sonarrTags = await _api.GetTags();
        await CreateMissingTags(sonarrTags, tags);
        return sonarrTags
            .Where(t => tags.Any(ct => ct.EqualsIgnoreCase(t.Label)))
            .Select(t => t.Id)
            .ToList();
    }

    private async Task DoVersionEnforcement()
    {
        var capabilities = await _compatibility.Capabilities.LastAsync();
        if (!capabilities.SupportsNamedReleaseProfiles)
        {
            throw new VersionException(
                $"Your Sonarr version {capabilities.Version} does not meet the minimum " +
                $"required version of {_compatibility.MinimumVersion} to use this program");
        }
    }

    private async Task CreateMissingTags(ICollection<SonarrTag> sonarrTags, IEnumerable<string> configTags)
    {
        var missingTags = configTags.Where(t => !sonarrTags.Any(t2 => t2.Label.EqualsIgnoreCase(t)));
        foreach (var tag in missingTags)
        {
            _log.Debug("Creating Tag: {Tag}", tag);
            var newTag = await _api.CreateTag(tag);
            sonarrTags.Add(newTag);
        }
    }

    private const string ProfileNamePrefix = "[Trash]";

    private static string BuildProfileTitle(string profileName)
    {
        return $"{ProfileNamePrefix} {profileName}";
    }

    private static SonarrReleaseProfile? GetProfileToUpdate(IEnumerable<SonarrReleaseProfile> profiles,
        string profileName)
    {
        return profiles.FirstOrDefault(p => p.Name == profileName);
    }

    private static void SetupProfileRequestObject(SonarrReleaseProfile profileToUpdate, ReleaseProfileData profile,
        IReadOnlyCollection<int> tagIds)
    {
        profileToUpdate.Preferred = profile.Preferred
            .SelectMany(x => x.Terms.Select(termData => new SonarrPreferredTerm(x.Score, termData.Term)))
            .ToList();

        profileToUpdate.Ignored = profile.Ignored.Select(x => x.Term).ToList();
        profileToUpdate.Required = profile.Required.Select(x => x.Term).ToList();
        profileToUpdate.IncludePreferredWhenRenaming = profile.IncludePreferredWhenRenaming;
        profileToUpdate.Tags = tagIds;
    }

    private async Task UpdateExistingProfile(SonarrReleaseProfile profileToUpdate, ReleaseProfileData profile,
        IReadOnlyCollection<int> tagIds)
    {
        _log.Debug("Update existing profile with id {ProfileId}", profileToUpdate.Id);
        SetupProfileRequestObject(profileToUpdate, profile, tagIds);
        await _api.UpdateReleaseProfile(profileToUpdate);
    }

    private async Task CreateNewProfile(string title, ReleaseProfileData profile, IReadOnlyCollection<int> tagIds)
    {
        var newProfile = new SonarrReleaseProfile {Name = title, Enabled = true};
        SetupProfileRequestObject(newProfile, profile, tagIds);
        await _api.CreateReleaseProfile(newProfile);
    }
}
