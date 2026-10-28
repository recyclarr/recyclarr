using Recyclarr.Config.Models;
using Recyclarr.Servarr.MediaManagement;
using Riok.Mapperly.Abstractions;
using SonarrApi = Recyclarr.Api.Sonarr.V3;

namespace Recyclarr.ServarrApi.MediaManagement;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
internal static partial class SonarrV3MediaManagementMapper
{
    [MapProperty(
        nameof(SonarrApi.MediaManagementConfigResource.DownloadPropersAndRepacks),
        nameof(MediaManagementData.PropersAndRepacks)
    )]
    public static partial MediaManagementData ToDomain(SonarrApi.MediaManagementConfigResource dto);

    public static void UpdateDto(
        MediaManagementData domain,
        SonarrApi.MediaManagementConfigResource dto
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
