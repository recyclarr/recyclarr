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

    [MapProperty(
        nameof(MediaManagementData.PropersAndRepacks),
        nameof(SonarrApi.MediaManagementSettingsResource.DownloadPropersAndRepacks)
    )]
    public static partial void UpdateDto(
        MediaManagementData domain,
        [MappingTarget] SonarrApi.MediaManagementSettingsResource dto
    );
}
