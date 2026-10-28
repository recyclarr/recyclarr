using Refit;

namespace Recyclarr.Api.Sonarr.V3;

/// <summary>Sonarr v3 media management settings (a single settings resource).</summary>
public interface IMediaManagementConfigApi
{
    [Get("/api/v3/config/mediamanagement")]
    Task<MediaManagementConfigResource> MediamanagementGet(CancellationToken ct = default);

    [Put("/api/v3/config/mediamanagement/{id}")]
    Task<MediaManagementConfigResource> MediamanagementPut(
        string id,
        [Body] MediaManagementConfigResource? body,
        CancellationToken ct = default
    );
}

/// <summary>Sonarr v3 file and folder naming settings (a single settings resource).</summary>
public interface INamingConfigApi
{
    [Get("/api/v3/config/naming")]
    Task<NamingConfigResource> NamingGet(CancellationToken ct = default);

    [Put("/api/v3/config/naming/{id}")]
    Task<NamingConfigResource> NamingPut(
        string id,
        [Body] NamingConfigResource? body,
        CancellationToken ct = default
    );
}

/// <summary>
/// Sonarr server information, used to detect the server version. Sonarr v4 and v5 both serve
/// this endpoint.
/// </summary>
public interface ISystemApi
{
    [Get("/api/v3/system/status")]
    Task<SystemResource> Status(CancellationToken ct = default);
}

/// <summary>
/// Media management settings. <see cref="DownloadPropersAndRepacks" /> is Sonarr's raw value
/// (for example <c>preferAndUpgrade</c>), kept as a string so new values round-trip.
/// </summary>
public class MediaManagementConfigResource : ExtensibleResource
{
    public int? Id { get; set; }
    public string? DownloadPropersAndRepacks { get; set; }
}

/// <summary>Episode file and series/season folder naming settings.</summary>
public class NamingConfigResource : ExtensibleResource
{
    public int? Id { get; set; }
    public bool? RenameEpisodes { get; set; }
    public string? StandardEpisodeFormat { get; set; }
    public string? DailyEpisodeFormat { get; set; }
    public string? AnimeEpisodeFormat { get; set; }
    public string? SeriesFolderFormat { get; set; }
    public string? SeasonFolderFormat { get; set; }
}

/// <summary>Server status. <see cref="Version" /> is a dotted version string.</summary>
public class SystemResource : ExtensibleResource
{
    public string? AppName { get; set; }
    public string? Version { get; set; }
}
