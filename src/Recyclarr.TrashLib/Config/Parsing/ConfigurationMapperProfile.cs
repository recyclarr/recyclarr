using AutoMapper;
using JetBrains.Annotations;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Config.Parsing;

[UsedImplicitly]
public class ConfigurationMapperProfile : Profile
{
    public ConfigurationMapperProfile()
    {
        CreateMap<QualityScoreConfigYamlLatest, QualityProfileScoreConfig>();
        CreateMap<CustomFormatConfigYamlLatest, CustomFormatConfig>();
        CreateMap<QualitySizeConfigYamlLatest, QualityDefinitionConfig>();
        CreateMap<QualityProfileConfigYamlLatest, QualityProfileConfig>();
        CreateMap<ReleaseProfileConfigYamlLatest, ReleaseProfileConfig>();
        CreateMap<ReleaseProfileFilterConfigYamlLatest, SonarrProfileFilterConfig>();

        CreateMap<ServiceConfigYamlLatest, ServiceConfiguration>()
            .ForMember(x => x.InstanceName, o => o.Ignore());

        CreateMap<RadarrConfigYamlLatest, RadarrConfiguration>()
            .IncludeBase<ServiceConfigYamlLatest, ServiceConfiguration>();

        CreateMap<SonarrConfigYamlLatest, SonarrConfiguration>()
            .IncludeBase<ServiceConfigYamlLatest, ServiceConfiguration>();
    }
}
