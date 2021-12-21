using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Common.Extensions;
using Serilog;
using TrashLib.ExceptionTypes;
using TrashLib.Sonarr.Api;
using TrashLib.Sonarr.Api.Objects;
using TrashLib.Sonarr.Config;

namespace TrashLib.Sonarr.ReleaseProfile;

internal class ReleaseProfileUpdater : IReleaseProfileUpdater
{
    private readonly ISonarrApi _api;
    private readonly ISonarrCompatibility _compatibility;
    private readonly IReleaseProfileGuideParser _parser;

    public ReleaseProfileUpdater(
        ILogger logger,
        IReleaseProfileGuideParser parser,
        ISonarrApi api,
        ISonarrCompatibility compatibility)
    {
        Log = logger;
        _parser = parser;
        _api = api;
        _compatibility = compatibility;
    }

    private ILogger Log { get; }

    public async Task Process(bool isPreview, SonarrConfiguration config)
    {
        foreach (var profile in config.ReleaseProfiles)
        {
            Log.Information("Processing Release Profile: {ProfileName}", profile.Type);
            var markdown = await _parser.GetMarkdownData(profile.Type);
            var profiles = Utils.FilterProfiles(_parser.ParseMarkdown(profile, markdown));

            if (profile.Filter.IncludeOptional)
            {
                Log.Information("Configuration is set to allow optional terms to be synchronized");
            }

            if (isPreview)
            {
                Utils.PrintTermsAndScores(profiles);
                continue;
            }

            await ProcessReleaseProfiles(profiles, profile);
        }
    }

    private async Task DoVersionEnforcement()
    {
        // _compatibility.Capabilities
        //     .Where(x => !x.SupportsNamedReleaseProfiles)
        //     .Subscribe(x => throw new VersionException(
        //         $"Your Sonarr version {x.Version} does not meet the minimum " +
        //         $"required version of {_compatibility.MinimumVersion} to use this program"));

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
            Log.Debug("Creating Tag: {Tag}", tag);
            var newTag = await _api.CreateTag(tag);
            sonarrTags.Add(newTag);
        }
    }

    private string BuildProfileTitle(ReleaseProfileType profileType, string profileName)
    {
        var titleType = profileType.ToString();
        return $"[Trash] {titleType} - {profileName}";
    }

    private static SonarrReleaseProfile? GetProfileToUpdate(IEnumerable<SonarrReleaseProfile> profiles,
        string profileName)
    {
        return profiles.FirstOrDefault(p => p.Name == profileName);
    }

    private static void SetupProfileRequestObject(SonarrReleaseProfile profileToUpdate, FilteredProfileData profile,
        List<int> tagIds)
    {
        profileToUpdate.Preferred = profile.Preferred
            .SelectMany(kvp => kvp.Value.Select(term => new SonarrPreferredTerm(kvp.Key, term)))
            .ToList();

        profileToUpdate.Ignored = profile.Ignored.ToList(); //string.Join(',', profile.Ignored);
        profileToUpdate.Required = profile.Required.ToList(); //string.Join(',', profile.Required);

        // Null means the guide didn't specify a value for this, so we leave the existing setting intact.
        if (profile.IncludePreferredWhenRenaming != null)
        {
            profileToUpdate.IncludePreferredWhenRenaming = profile.IncludePreferredWhenRenaming.Value;
        }

        profileToUpdate.Tags = tagIds;
    }

    private async Task UpdateExistingProfile(SonarrReleaseProfile profileToUpdate, FilteredProfileData profile,
        List<int> tagIds)
    {
        Log.Debug("Update existing profile with id {ProfileId}", profileToUpdate.Id);
        SetupProfileRequestObject(profileToUpdate, profile, tagIds);
        await _api.UpdateReleaseProfile(profileToUpdate);
    }

    private async Task CreateNewProfile(string title, FilteredProfileData profile, List<int> tagIds)
    {
        var newProfile = new SonarrReleaseProfile
        {
            Name = title,
            Enabled = true
        };

        SetupProfileRequestObject(newProfile, profile, tagIds);
        await _api.CreateReleaseProfile(newProfile);
    }

    private async Task ProcessReleaseProfiles(IDictionary<string, ProfileData> profiles,
        ReleaseProfileConfig config)
    {
        await DoVersionEnforcement();

        List<int> tagIds = new();

        // If tags were provided, ensure they exist. Tags that do not exist are added first, so that we
        // may specify them with the release profile request payload.
        if (config.Tags.Count > 0)
        {
            var sonarrTags = await _api.GetTags();
            await CreateMissingTags(sonarrTags, config.Tags);
            tagIds = sonarrTags.Where(t => config.Tags.Any(ct => ct.EqualsIgnoreCase(t.Label)))
                .Select(t => t.Id)
                .ToList();
        }

        // Obtain all of the existing release profiles first. If any were previously created by our program
        // here, we favor replacing those instead of creating new ones, which would just be mostly duplicates
        // (but with some differences, since there have likely been updates since the last run).
        var existingProfiles = await _api.GetReleaseProfiles();

        foreach (var (name, profileData) in profiles)
        {
            var filteredProfileData = new FilteredProfileData(profileData, config);
            var title = BuildProfileTitle(config.Type, name);
            var profileToUpdate = GetProfileToUpdate(existingProfiles, title);
            if (profileToUpdate != null)
            {
                Log.Information("Update existing profile: {ProfileName}", title);
                await UpdateExistingProfile(profileToUpdate, filteredProfileData, tagIds);
            }
            else
            {
                Log.Information("Create new profile: {ProfileName}", title);
                await CreateNewProfile(title, filteredProfileData, tagIds);
            }
        }
    }
}
