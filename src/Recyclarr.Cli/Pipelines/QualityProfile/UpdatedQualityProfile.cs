using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile;

internal record UpdatedQualities
{
    public ICollection<string> InvalidQualityNames { get; init; } = [];
    public IReadOnlyCollection<ProfileItemDto> Items { get; init; } = [];
    public int NumWantedItems { get; init; }
}

internal record UpdatedQualityProfile
{
    public required QualityProfileDto ProfileDto { get; set; }
    public required PlannedQualityProfile ProfileConfig { get; init; }
    public IReadOnlyList<ProfileLanguageDto> Languages { get; init; } = [];
    public IReadOnlyCollection<UpdatedFormatScore> UpdatedScores { get; set; } = [];
    public UpdatedQualities UpdatedQualities { get; init; } = new();
    public IReadOnlyCollection<string> InvalidExceptCfNames { get; set; } = [];
    public IReadOnlyCollection<string> MissingQualities { get; set; } = [];

    public string ProfileName
    {
        get
        {
            var name = ProfileDto.Name;
            if (string.IsNullOrEmpty(name))
            {
                name = ProfileConfig.Config.Name;
            }

            return name;
        }
    }

    public string? TrashId => ProfileConfig.Resource?.TrashId;

    // Effective value resolution: config override > guide resource > service DTO.
    // These are the single source of truth for what gets sent to the API and validated.
    public string EffectiveName
    {
        get
        {
            var config = ProfileConfig.Config;
            var resource = ProfileConfig.Resource;
            return !string.IsNullOrEmpty(config.Name)
                ? config.Name
                : resource?.Name ?? ProfileDto.Name;
        }
    }

    public bool? EffectiveUpgradeAllowed =>
        ProfileConfig.Config.UpgradeAllowed
        ?? ProfileConfig.Resource?.UpgradeAllowed
        ?? ProfileDto.UpgradeAllowed;

    public int? EffectiveMinFormatScore =>
        ProfileConfig.Config.MinFormatScore
        ?? ProfileConfig.Resource?.MinFormatScore
        ?? ProfileDto.MinFormatScore;

    public int? EffectiveMinUpgradeFormatScore =>
        ProfileConfig.Config.MinUpgradeFormatScore
        ?? ProfileConfig.Resource?.MinUpgradeFormatScore
        ?? ProfileDto.MinUpgradeFormatScore;

    public int? EffectiveCutoffFormatScore =>
        ProfileConfig.Config.UpgradeUntilScore
        ?? ProfileConfig.Resource?.CutoffFormatScore
        ?? ProfileDto.CutoffFormatScore;

    public QualityProfileDto BuildUpdatedDto()
    {
        var newDto = ProfileDto with
        {
            Name = EffectiveName,
            UpgradeAllowed = EffectiveUpgradeAllowed,
            MinFormatScore = EffectiveMinFormatScore,
            MinUpgradeFormatScore = EffectiveMinUpgradeFormatScore,
            CutoffFormatScore = EffectiveCutoffFormatScore,
            FormatItems = UpdatedScores.Select(x => x.Dto with { Score = x.NewScore }).ToList(),
        };

        if (UpdatedQualities.NumWantedItems > 0)
        {
            newDto = newDto with { Items = UpdatedQualities.Items };
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
        if (newDto.Cutoff is null || effectiveCutoff is not null)
        {
            newDto = newDto with
            {
                Cutoff = newDto.Items.FindCutoff(effectiveCutoff) ?? newDto.Items.FirstCutoffId(),
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
                newDto = newDto with { Language = language };
            }
        }

        return newDto;
    }
}
