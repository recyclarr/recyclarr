using Refit;

namespace Recyclarr.Api.Sonarr.V5;

/// <summary>Sonarr v5 media management settings (a single settings resource).</summary>
public interface IMediaManagementSettingsApi
{
    [Get("/api/v5/settings/mediamanagement")]
    Task<MediaManagementSettingsResource> MediamanagementGet(CancellationToken ct = default);

    [Put("/api/v5/settings/mediamanagement/{id}")]
    Task<MediaManagementSettingsResource> MediamanagementPut(
        string id,
        [Body] MediaManagementSettingsResource? body,
        CancellationToken ct = default
    );
}

/// <summary>Sonarr v5 file and folder naming settings (a single settings resource).</summary>
public interface INamingSettingsApi
{
    [Get("/api/v5/settings/naming")]
    Task<NamingSettingsResource> NamingGet(CancellationToken ct = default);

    [Put("/api/v5/settings/naming/{id}")]
    Task<NamingSettingsResource> NamingPut(
        string id,
        [Body] NamingSettingsResource? body,
        CancellationToken ct = default
    );
}

/// <summary>
/// Media management settings. <see cref="DownloadPropersAndRepacks" /> is Sonarr's raw value
/// (for example <c>preferAndUpgrade</c>), kept as a string so new values round-trip.
/// </summary>
public class MediaManagementSettingsResource : ExtensibleResource
{
    public int? Id { get; set; }
    public string? DownloadPropersAndRepacks { get; set; }
}

/// <summary>Episode file and series/season folder naming settings.</summary>
public class NamingSettingsResource : ExtensibleResource
{
    public int? Id { get; set; }
    public bool? RenameEpisodes { get; set; }
    public string? StandardEpisodeFormat { get; set; }
    public string? DailyEpisodeFormat { get; set; }
    public string? AnimeEpisodeFormat { get; set; }
    public string? SeriesFolderFormat { get; set; }
    public string? SeasonFolderFormat { get; set; }
}
