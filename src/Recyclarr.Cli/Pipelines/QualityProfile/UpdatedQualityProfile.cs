using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Servarr.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile;

internal record UpdatedQualities
{
    public ICollection<string> InvalidQualityNames { get; init; } = [];
    public IReadOnlyList<QualityProfileItem> Items { get; init; } = [];
    public int NumWantedItems { get; init; }
}

internal record UpdatedQualityProfile
{
    public required QualityProfileData Profile { get; set; }
    public required PlannedQualityProfile ProfileConfig { get; init; }
    public IReadOnlyList<ProfileLanguage> Languages { get; init; } = [];
    public IReadOnlyCollection<UpdatedFormatScore> UpdatedScores { get; set; } = [];
    public UpdatedQualities UpdatedQualities { get; init; } = new();
    public IReadOnlyCollection<string> InvalidExceptCfNames { get; set; } = [];
    public IReadOnlyCollection<string> MissingQualities { get; set; } = [];

    public string ProfileName
    {
        get
        {
            var name = Profile.Name;
            if (string.IsNullOrEmpty(name))
            {
                name = ProfileConfig.Config.Name;
            }

            return name;
        }
    }

    public string? TrashId => ProfileConfig.Resource?.TrashId;

    // Effective value resolution: config override > guide resource > service profile.
    // These are the single source of truth for what gets sent to the API and validated.
    public string EffectiveName
    {
        get
        {
            var config = ProfileConfig.Config;
            var resource = ProfileConfig.Resource;
            return !string.IsNullOrEmpty(config.Name)
                ? config.Name
                : resource?.Name ?? Profile.Name;
        }
    }

    public bool? EffectiveUpgradeAllowed =>
        ProfileConfig.Config.UpgradeAllowed
        ?? ProfileConfig.Resource?.UpgradeAllowed
        ?? Profile.UpgradeAllowed;

    public int? EffectiveMinFormatScore =>
        ProfileConfig.Config.MinFormatScore
        ?? ProfileConfig.Resource?.MinFormatScore
        ?? Profile.MinFormatScore;

    public int? EffectiveMinUpgradeFormatScore =>
        ProfileConfig.Config.MinUpgradeFormatScore
        ?? ProfileConfig.Resource?.MinUpgradeFormatScore
        ?? Profile.MinUpgradeFormatScore;

    public int? EffectiveCutoffFormatScore =>
        ProfileConfig.Config.UpgradeUntilScore
        ?? ProfileConfig.Resource?.CutoffFormatScore
        ?? Profile.CutoffFormatScore;

    public QualityProfileData BuildMergedProfile()
    {
        var merged = Profile with
        {
            Name = EffectiveName,
            UpgradeAllowed = EffectiveUpgradeAllowed,
            MinFormatScore = EffectiveMinFormatScore,
            MinUpgradeFormatScore = EffectiveMinUpgradeFormatScore,
            CutoffFormatScore = EffectiveCutoffFormatScore,
            FormatItems = UpdatedScores
                .Select(x => x.FormatItem with { Score = x.NewScore })
                .ToList(),
        };

        if (UpdatedQualities.NumWantedItems > 0)
        {
            merged = merged with { Items = UpdatedQualities.Items };
        }

        // The `qualityprofile` API will still validate `cutoff` even when `upgradeAllowed` is set
        // to `false`. Because of this, we cannot set cutoff to null. We pick the first available if
        // the user didn't specify one.
        //
        // Also: It's important that we assign the cutoff *after* we set Items. Because we pull from
        // a different list of items depending on if the `qualities` property is set in config.
        //
        // In the case a quality profile is automatically added to the list of quality profiles (by
        // specifying it in `assign_scores_to` without explicitly having it in the
        // `quality_profiles` list), we only mutate the cutoff if it's not set.
        //
        // Additionally, there's no point in assigning a cutoff if the user didn't specify one in
        // their config.
        var effectiveCutoff =
            ProfileConfig.Config.UpgradeUntilQuality ?? ProfileConfig.Resource?.Cutoff;
        if (merged.Cutoff is null || effectiveCutoff is not null)
        {
            merged = merged with
            {
                Cutoff = merged.Items.FindCutoff(effectiveCutoff) ?? merged.Items.FirstCutoffId(),
            };
        }

        // Language passthrough from guide resource
        var resourceLanguage = ProfileConfig.Resource?.Language;
        if (!string.IsNullOrEmpty(resourceLanguage))
        {
            var language = Languages.FirstOrDefault(l =>
                l.Name.Equals(resourceLanguage, StringComparison.OrdinalIgnoreCase)
            );
            if (language is not null)
            {
                merged = merged with { Language = language };
            }
        }

        return merged;
    }
}
