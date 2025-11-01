using Recyclarr.Cli.Pipelines.QualityProfile.Models;
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
    public required QualityProfileDto ProfileDto { get; init; }
    public required ProcessedQualityProfileData ProfileConfig { get; init; }
    public required QualityProfileUpdateReason UpdateReason { get; set; }
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
                name = ProfileConfig.Profile.Name;
            }

            return name;
        }
    }

    public QualityProfileDto BuildUpdatedDto()
    {
        var config = ProfileConfig.Profile;
        var newDto = ProfileDto with
        {
            Name = config.Name, // Must keep this for NEW profile syncing. It will only assign if src is not null.
            UpgradeAllowed = config.UpgradeAllowed,
            MinFormatScore = config.MinFormatScore,
            MinFormatUpgradeScore = config.MinFormatUpgradeScore,
            CutoffFormatScore = config.UpgradeUntilScore,
            FormatItems = UpdatedScores.Select(x => x.Dto with { Score = x.NewScore }).ToList(),
        };

        if (UpdatedQualities.NumWantedItems > 0)
        {
            newDto.Items = UpdatedQualities.Items;
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
        if (newDto.Cutoff is null || config.UpgradeUntilQuality is not null)
        {
            newDto.Cutoff =
                newDto.Items.FindCutoff(config.UpgradeUntilQuality) ?? newDto.Items.FirstCutoffId();
        }

        return newDto;
    }
}
