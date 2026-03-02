using Recyclarr.Servarr.SystemStatus;
using Riok.Mapperly.Abstractions;
using SonarrApi = Recyclarr.Api.Sonarr;

namespace Recyclarr.ServarrApi.System;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
internal static partial class SonarrSystemMapper
{
    public static partial SystemServiceResult ToDomain(SonarrApi.SystemResource dto);

    private static Version StringToVersion(string version) => new(version);
}
