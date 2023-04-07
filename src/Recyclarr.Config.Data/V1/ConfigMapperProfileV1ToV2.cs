using AutoMapper;
using JetBrains.Annotations;

namespace Recyclarr.Config.Data.V1;

[UsedImplicitly]
public class ConfigMapperProfileV1ToV2 : Profile
{
    private static int _instanceNameCounter = 1;

    private static string BuildInstanceName()
    {
        return $"instance{_instanceNameCounter++}";
    }

    private sealed class ListToMapConverter<TOld, TNew>
        : IValueConverter<IReadOnlyCollection<TOld>, IReadOnlyDictionary<string, TNew>>
    {
        public IReadOnlyDictionary<string, TNew> Convert(
            IReadOnlyCollection<TOld>? sourceMember,
            ResolutionContext context)
        {
            return sourceMember?.ToDictionary(_ => BuildInstanceName(), y => context.Mapper.Map<TNew>(y)) ??
                new Dictionary<string, TNew>();
        }
    }

    public ConfigMapperProfileV1ToV2()
    {
        CreateMap<QualityScoreConfigYaml, V2.QualityScoreConfigYaml>();
        CreateMap<CustomFormatConfigYaml, V2.CustomFormatConfigYaml>();
        CreateMap<QualitySizeConfigYaml, V2.QualitySizeConfigYaml>();
        CreateMap<QualityProfileConfigYaml, V2.QualityProfileConfigYaml>();
        CreateMap<ServiceConfigYaml, V2.ServiceConfigYaml>();
        CreateMap<ReleaseProfileFilterConfigYaml, V2.ReleaseProfileFilterConfigYaml>();
        CreateMap<ReleaseProfileConfigYaml, V2.ReleaseProfileConfigYaml>();
        CreateMap<RadarrConfigYaml, V2.RadarrConfigYaml>();
        CreateMap<SonarrConfigYaml, V2.SonarrConfigYaml>();

        // Backward Compatibility: Convert list-based instances to mapping-based ones.
        // The key is auto-generated.
        CreateMap<RootConfigYaml, V2.RootConfigYaml>()
            .ForMember(x => x.Radarr, o => o
                .ConvertUsing(new ListToMapConverter<RadarrConfigYaml, V2.RadarrConfigYaml>()))
            .ForMember(x => x.Sonarr, o => o
                .ConvertUsing(new ListToMapConverter<SonarrConfigYaml, V2.SonarrConfigYaml>()))
            .ForMember(x => x.RadarrValues, o => o.Ignore())
            .ForMember(x => x.SonarrValues, o => o.Ignore())
            ;
    }
}
