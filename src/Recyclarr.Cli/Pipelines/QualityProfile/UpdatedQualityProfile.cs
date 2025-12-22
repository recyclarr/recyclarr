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

    public QualityProfileDto BuildUpdatedDto()
    {
        var config = ProfileConfig.Config;
        var resource = ProfileConfig.Resource;

        // Build effective values: Service DTO -> Guide Resource -> Config overrides
        // Each layer only applies if it has a value

        // Effective name: config override > guide resource > service DTO
        var effectiveName = !string.IsNullOrEmpty(config.Name)
            ? config.Name
            : resource?.Name ?? ProfileDto.Name;

        // Effective values: config override > guide resource > service DTO
        var effectiveUpgradeAllowed =
            config.UpgradeAllowed ?? resource?.UpgradeAllowed ?? ProfileDto.UpgradeAllowed;
        var effectiveMinFormatScore =
            config.MinFormatScore ?? resource?.MinFormatScore ?? ProfileDto.MinFormatScore;
        var effectiveMinUpgradeFormatScore =
            config.MinUpgradeFormatScore
            ?? resource?.MinUpgradeFormatScore
            ?? ProfileDto.MinUpgradeFormatScore;
        var effectiveCutoffFormatScore =
            config.UpgradeUntilScore ?? resource?.CutoffFormatScore ?? ProfileDto.CutoffFormatScore;

        var newDto = ProfileDto with
        {
            Name = effectiveName,
            UpgradeAllowed = effectiveUpgradeAllowed,
            MinFormatScore = effectiveMinFormatScore,
            MinUpgradeFormatScore = effectiveMinUpgradeFormatScore,
            CutoffFormatScore = effectiveCutoffFormatScore,
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
        var effectiveCutoff = config.UpgradeUntilQuality ?? resource?.Cutoff;
        if (newDto.Cutoff is null || effectiveCutoff is not null)
        {
            newDto = newDto with
            {
                Cutoff = newDto.Items.FindCutoff(effectiveCutoff) ?? newDto.Items.FirstCutoffId(),
            };
        }

        // Language passthrough from guide resource
        if (!string.IsNullOrEmpty(resource?.Language))
        {
            var language = Languages.FirstOrDefault(l =>
                l.Name.Equals(resource.Language, StringComparison.OrdinalIgnoreCase)
            );
            if (language is not null)
            {
                newDto = newDto with { Language = language };
            }
        }

        return newDto;
    }
}
