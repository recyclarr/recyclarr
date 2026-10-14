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

    [MapProperty(
        nameof(MediaManagementData.PropersAndRepacks),
        nameof(SonarrApi.MediaManagementConfigResource.DownloadPropersAndRepacks)
    )]
    public static partial void UpdateDto(
        MediaManagementData domain,
        [MappingTarget] SonarrApi.MediaManagementConfigResource dto
    );
}
