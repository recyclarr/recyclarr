using Recyclarr.Config.Models;
using Recyclarr.Servarr.MediaManagement;
using Riok.Mapperly.Abstractions;
using SonarrApi = Recyclarr.Api.Sonarr.V5;

namespace Recyclarr.ServarrApi.MediaManagement;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
internal static partial class SonarrV5MediaManagementMapper
{
    [MapProperty(
        nameof(SonarrApi.MediaManagementSettingsResource.DownloadPropersAndRepacks),
        nameof(MediaManagementData.PropersAndRepacks)
    )]
    public static partial MediaManagementData ToDomain(
        SonarrApi.MediaManagementSettingsResource dto
    );

    public static void UpdateDto(
        MediaManagementData domain,
        SonarrApi.MediaManagementSettingsResource dto
    )
    {
        if (domain.PropersAndRepacks is { } mode)
        {
            dto.DownloadPropersAndRepacks = PropersAndRepacksValue.Format(mode);
        }
    }

    private static PropersAndRepacksMode? ParsePropers(string? value) =>
        PropersAndRepacksValue.Parse(value);
}
