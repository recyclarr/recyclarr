using Recyclarr.Config.Models;
using Recyclarr.Servarr.MediaManagement;
using Riok.Mapperly.Abstractions;
using RadarrApi = Recyclarr.Api.Radarr;

namespace Recyclarr.ServarrApi.MediaManagement;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
internal static partial class RadarrMediaManagementMapper
{
    [MapProperty(
        nameof(RadarrApi.MediaManagementConfigResource.DownloadPropersAndRepacks),
        nameof(MediaManagementData.PropersAndRepacks)
    )]
    public static partial MediaManagementData ToDomain(RadarrApi.MediaManagementConfigResource dto);

    public static void UpdateDto(
        MediaManagementData domain,
        RadarrApi.MediaManagementConfigResource dto
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
