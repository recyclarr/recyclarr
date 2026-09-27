using System.Text.Json.Serialization;

namespace Recyclarr.Server.Features.Guide.Naming;

// Sonarr and Radarr name different things, so each service has its own set of format lists.
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

// Key is the name used in configuration. Version is set when the format applies only to that
// major version of the service; null means every version.
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GuideNamingFormatResponse(string Key, string? Version, string Format);
