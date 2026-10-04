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
}

/// <summary>
/// A Radarr quality profile. Radarr profiles have a language; Sonarr profiles do not (ADR-023).
/// Shared pipeline code transforms profiles with <c>with</c> expressions, which keep this runtime
/// type, so the language survives from fetch through persist.
/// </summary>
public record RadarrQualityProfileData : QualityProfileData
{
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

    /// <summary>
    /// Size limits stored on this quality item by services that keep sizes per profile (Sonarr
    /// v5). Null means the service reported no sizes, or the item came from a source that does not
    /// carry them; gateways then leave the service's values untouched.
    /// </summary>
    public QualityItemSize? Size { get; init; }
}

/// <summary>
/// Quality size limits in MB per minute. A null <see cref="Max"/> or <see cref="Preferred"/> means
/// unlimited.
/// </summary>
/// <remarks>
/// Values are stored without trailing decimal zeros, so equal sizes serialize identically (for
/// example <c>70.0</c> becomes <c>70</c>). Change detection compares serialized items, and
/// computed sizes would otherwise never match the values a service returns.
/// </remarks>
public record QualityItemSize
{
    public QualityItemSize(decimal min, decimal? max, decimal? preferred)
    {
        Min = Normalize(min);
        Max = max is { } m ? Normalize(m) : null;
        Preferred = preferred is { } p ? Normalize(p) : null;
    }

    public decimal Min { get; }
    public decimal? Max { get; }
    public decimal? Preferred { get; }

    // Dividing by 1 with maximum scale strips trailing zeros without changing the value
    private static decimal Normalize(decimal value) => value / 1.0000000000000000000000000000m;
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
