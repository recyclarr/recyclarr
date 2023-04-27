using AutoMapper;
using JetBrains.Annotations;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Config.Parsing;

[UsedImplicitly]
public class ConfigurationMapperProfile : Profile
{
    public ConfigurationMapperProfile()
    {
        CreateMap<QualityScoreConfigYaml, QualityProfileScoreConfig>();
        CreateMap<CustomFormatConfigYaml, CustomFormatConfig>();
        CreateMap<QualitySizeConfigYaml, QualityDefinitionConfig>();
        CreateMap<QualityProfileConfigYaml, QualityProfileConfig>();
        CreateMap<ReleaseProfileConfigYaml, ReleaseProfileConfig>();
        CreateMap<ReleaseProfileFilterConfigYaml, SonarrProfileFilterConfig>();

        CreateMap<ServiceConfigYaml, ServiceConfiguration>()
            .ForMember(x => x.InstanceName, o => o.Ignore());

        CreateMap<RadarrConfigYaml, RadarrConfiguration>()
            .IncludeBase<ServiceConfigYaml, ServiceConfiguration>();

        CreateMap<SonarrConfigYaml, SonarrConfiguration>()
            .IncludeBase<ServiceConfigYaml, ServiceConfiguration>();
    }
}
