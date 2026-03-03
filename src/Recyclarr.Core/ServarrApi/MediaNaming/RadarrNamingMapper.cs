using Recyclarr.Servarr.MediaNaming;
using Riok.Mapperly.Abstractions;
using RadarrApi = Recyclarr.Api.Radarr;

namespace Recyclarr.ServarrApi.MediaNaming;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
internal static partial class RadarrNamingMapper
{
    public static partial RadarrNamingData ToDomain(RadarrApi.NamingConfigResource dto);

    public static partial void UpdateDto(
        RadarrNamingData domain,
        [MappingTarget] RadarrApi.NamingConfigResource dto
    );
}
