using Recyclarr.Config.Models;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Servarr.QualityProfile;

namespace Recyclarr.Pipelines.QualityProfile;

public record UpdatedQualities
{
    public ICollection<string> InvalidQualityNames { get; init; } = [];
    public IReadOnlyList<QualityProfileItem> Items { get; init; } = [];
    public int NumWantedItems { get; init; }
}

public record UpdatedQualityProfile
{
    public required QualityProfileData Profile { get; set; }
    internal QualityProfileData? OriginalProfile { get; init; }
    internal PlannedQualityProfile ProfileConfig { get; init; } = null!;
    public IReadOnlyCollection<UpdatedFormatScore> UpdatedScores { get; set; } = [];
    public UpdatedQualities UpdatedQualities { get; init; } = new();
    public IReadOnlyCollection<string> InvalidExceptCfNames { get; set; } = [];
    public IReadOnlyCollection<string> InvalidExceptCfPatterns { get; set; } = [];
    public IReadOnlyCollection<string> MissingQualities { get; set; } = [];

    public bool HasQualityOverrides => ProfileConfig.Config.Qualities.Count != 0;
    public QualitySortAlgorithm QualitySort => ProfileConfig.Config.QualitySort;

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

    // Effective value resolution: config override > guide resource > service profile.
    // These are the single source of truth for what gets sent to the API and validated.
    public string EffectiveName
    {
        get
        {
            var config = ProfileConfig.Config;
            var resource = ProfileConfig.GuideResource;
            return !string.IsNullOrEmpty(config.Name)
                ? config.Name
                : resource?.Name ?? Profile.Name;
        }
    }

    public bool? EffectiveUpgradeAllowed => ManagedUpgradeAllowed ?? Profile.UpgradeAllowed;
    public int? EffectiveMinFormatScore => ManagedMinFormatScore ?? Profile.MinFormatScore;

    public int? EffectiveMinUpgradeFormatScore =>
        ManagedMinUpgradeFormatScore ?? Profile.MinUpgradeFormatScore;

    public int? EffectiveCutoffFormatScore => ManagedCutoffFormatScore ?? Profile.CutoffFormatScore;

    // Managed values: what configuration or guide data decides, null when neither does and the
    // service value stays.
    internal bool? ManagedUpgradeAllowed =>
        ProfileConfig.Config.UpgradeAllowed ?? ProfileConfig.GuideResource?.UpgradeAllowed;

    internal int? ManagedMinFormatScore =>
        ProfileConfig.Config.MinFormatScore ?? ProfileConfig.GuideResource?.MinFormatScore;

    internal int? ManagedMinUpgradeFormatScore =>
        ProfileConfig.Config.MinUpgradeFormatScore
        ?? ProfileConfig.GuideResource?.MinUpgradeFormatScore;

    internal int? ManagedCutoffFormatScore =>
        ProfileConfig.Config.UpgradeUntilScore ?? ProfileConfig.GuideResource?.CutoffFormatScore;

    /// <summary>
    /// Configured qualities manage the layout. Without them, Recyclarr still manages it when it
    /// adds qualities the service has but the profile lacks.
    /// </summary>
    internal bool QualitiesManaged => ConfiguresQualities || MissingQualities.Count > 0;

    private bool ConfiguresQualities => UpdatedQualities.NumWantedItems > 0;

    /// <summary>
    /// The service requires a cutoff, so Recyclarr also manages it when nothing configures one and
    /// the profile has none; it then picks the first allowed quality.
    /// </summary>
    internal bool CutoffManaged => ManagedCutoffName is not null || Profile.Cutoff is null;

    private string? ManagedCutoffName =>
        ProfileConfig.Config.UpgradeUntilQuality ?? ProfileConfig.GuideResource?.Cutoff;

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

        if (ConfiguresQualities)
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
        if (CutoffManaged)
        {
            merged = merged with
            {
                Cutoff = merged.Items.FindCutoff(ManagedCutoffName) ?? merged.Items.FirstCutoffId(),
            };
        }

        return merged;
    }
}
