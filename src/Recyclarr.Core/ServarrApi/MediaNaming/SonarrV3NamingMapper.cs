using Recyclarr.Servarr.MediaNaming;
using Riok.Mapperly.Abstractions;
using SonarrApi = Recyclarr.Api.Sonarr.V3;

namespace Recyclarr.ServarrApi.MediaNaming;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
internal static partial class SonarrV3NamingMapper
{
    public static partial SonarrNamingData ToDomain(SonarrApi.NamingConfigResource dto);

    public static partial void UpdateDto(
        SonarrNamingData domain,
        [MappingTarget] SonarrApi.NamingConfigResource dto
    );
}
