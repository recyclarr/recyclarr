using Recyclarr.Servarr.SystemStatus;
using Riok.Mapperly.Abstractions;
using RadarrApi = Recyclarr.Api.Radarr;

namespace Recyclarr.ServarrApi.System;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
internal static partial class RadarrSystemMapper
{
    public static partial SystemServiceResult ToDomain(RadarrApi.SystemResource dto);

    private static Version StringToVersion(string version) => new(version);
}
