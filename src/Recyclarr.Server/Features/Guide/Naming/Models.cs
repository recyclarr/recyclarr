using System.Text.Json.Serialization;

namespace Recyclarr.Server.Features.Guide.Naming;

/// <summary>
/// Guide naming formats. Sonarr and Radarr name different things, so the `service` property
/// selects which set of format lists is present.
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "service")]
[JsonDerivedType(typeof(RadarrGuideNamingResponse), "radarr")]
[JsonDerivedType(typeof(SonarrGuideNamingResponse), "sonarr")]
internal abstract record GuideNamingResponse;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record RadarrGuideNamingResponse(
    IReadOnlyList<GuideNamingFormatResponse> MovieFolder,
    IReadOnlyList<GuideNamingFormatResponse> StandardMovie
) : GuideNamingResponse;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record SonarrGuideNamingResponse(
    IReadOnlyList<GuideNamingFormatResponse> SeasonFolder,
    IReadOnlyList<GuideNamingFormatResponse> SeriesFolder,
    IReadOnlyList<GuideNamingFormatResponse> StandardEpisode,
    IReadOnlyList<GuideNamingFormatResponse> DailyEpisode,
    IReadOnlyList<GuideNamingFormatResponse> AnimeEpisode
) : GuideNamingResponse;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GuideNamingFormatResponse
{
    /// <summary>The name that selects this format in configuration.</summary>
    public required string Key { get; init; }

    /// <summary>
    /// The major version of the service the format is for; absent when it applies to every
    /// version.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>The naming format string, in the service's naming token syntax.</summary>
    public required string Format { get; init; }
}
