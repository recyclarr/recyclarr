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

    [MapProperty(
        nameof(MediaManagementData.PropersAndRepacks),
        nameof(RadarrApi.MediaManagementConfigResource.DownloadPropersAndRepacks)
    )]
    public static partial void UpdateDto(
        MediaManagementData domain,
        [MappingTarget] RadarrApi.MediaManagementConfigResource dto
    );
}
