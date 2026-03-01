using Recyclarr.SyncState;

namespace Recyclarr.Servarr.QualityProfile;

public record QualityProfileData : IServiceResource
{
    public int? Id { get; init; }

    // Explicit interface implementation - only valid for profiles fetched from service (which always have Id)
    int IServiceResource.Id =>
        Id ?? throw new InvalidOperationException("QualityProfileData.Id is null");

    public required string Name { get; init; }
    public bool? UpgradeAllowed { get; init; }
    public int? MinFormatScore { get; init; }
    public int? MinUpgradeFormatScore { get; init; }
    public int? Cutoff { get; init; }
    public int? CutoffFormatScore { get; init; }
    public IReadOnlyList<QualityProfileFormatItem> FormatItems { get; init; } = [];
    public IReadOnlyList<QualityProfileItem> Items { get; init; } = [];
    public ProfileLanguage? Language { get; init; }
}

public record QualityProfileFormatItem
{
    public required int FormatId { get; init; }
    public string Name { get; init; } = "";
    public int Score { get; init; }
}

public record QualityProfileItem
{
    public int? Id { get; init; }
    public string? Name { get; init; }
    public bool? Allowed { get; init; }
    public QualityProfileItemQuality? Quality { get; init; }
    public IReadOnlyList<QualityProfileItem> Items { get; init; } = [];
}

public record QualityProfileItemQuality
{
    public int? Id { get; init; }
    public string? Name { get; init; }
}

public record ProfileLanguage
{
    public required int Id { get; init; }
    public required string Name { get; init; }
}
