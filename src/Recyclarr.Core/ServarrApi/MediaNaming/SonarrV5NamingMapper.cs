using Recyclarr.Servarr.MediaNaming;
using Riok.Mapperly.Abstractions;
using SonarrApi = Recyclarr.Api.Sonarr.V5;

namespace Recyclarr.ServarrApi.MediaNaming;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
internal static partial class SonarrV5NamingMapper
{
    public static partial SonarrNamingData ToDomain(SonarrApi.NamingSettingsResource dto);

    public static partial void UpdateDto(
        SonarrNamingData domain,
        [MappingTarget] SonarrApi.NamingSettingsResource dto
    );
}
