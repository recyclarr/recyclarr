using Recyclarr.Cli.Pipelines.QualityProfile.Api;
using Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

namespace Recyclarr.Cli.Pipelines.QualityProfile;

public record UpdatedQualities
{
    public ICollection<string> InvalidQualityNames { get; init; } = new List<string>();
    public IReadOnlyCollection<ProfileItemDto> Items { get; init; } = new List<ProfileItemDto>();
}

public record UpdatedQualityProfile
{
    public required QualityProfileDto ProfileDto { get; init; }
    public required ProcessedQualityProfileData ProfileConfig { get; init; }
    public required QualityProfileUpdateReason UpdateReason { get; set; }
    public IReadOnlyCollection<UpdatedFormatScore> UpdatedScores { get; set; } = Array.Empty<UpdatedFormatScore>();
    public UpdatedQualities UpdatedQualities { get; init; } = new();

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

        // The `qualityprofile` API will still validate `cutoff` even when `upgradeAllowed` is set to `false`.
        // Because of this, we cannot set cutoff to null. We pick the first available if the user didn't specify one.
        var cutoff = config.UpgradeAllowed
            ? UpdatedQualities.Items.FindCutoff(config.UpgradeUntilQuality)
            : UpdatedQualities.Items.First().Id;

        return ProfileDto with
        {
            Name = config.Name, // Must keep this for NEW profile syncing. It will only assign if src is not null.
            UpgradeAllowed = config.UpgradeAllowed,
            MinFormatScore = config.MinFormatScore,
            Cutoff = cutoff,
            CutoffFormatScore = config.UpgradeUntilScore,
            FormatItems = UpdatedScores.Select(x => x.Dto with {Score = x.NewScore}).ToList(),
            Items = UpdatedQualities.Items
        };
    }
}
