using AutoMapper;
using JetBrains.Annotations;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Config.Parsing;

[UsedImplicitly]
public class ConfigYamlMapperProfile : Profile
{
    public ConfigYamlMapperProfile()
    {
        CreateMap<QualityScoreConfigYaml, QualityProfileScoreConfig>();
        CreateMap<CustomFormatConfigYaml, CustomFormatConfig>();
        CreateMap<QualitySizeConfigYaml, QualityDefinitionConfig>();
        CreateMap<ReleaseProfileConfigYaml, ReleaseProfileConfig>();
        CreateMap<ReleaseProfileFilterConfigYaml, SonarrProfileFilterConfig>();
        CreateMap<QualityProfileQualityConfigYaml, QualityProfileQualityConfig>()
            .ForMember(x => x.Enabled, o => o.NullSubstitute(true));

        CreateMap<ResetUnmatchedScoresConfigYaml, ResetUnmatchedScoresConfig>();

        CreateMap<QualityProfileConfigYaml, QualityProfileConfig>()
            .ForMember(x => x.UpgradeAllowed, o => o.MapFrom(x => x.Upgrade!.Allowed))
            .ForMember(x => x.UpgradeUntilQuality, o => o.MapFrom(x => x.Upgrade!.UntilQuality))
            .ForMember(x => x.UpgradeUntilScore, o => o.MapFrom(x => x.Upgrade!.UntilScore))
            .ForMember(x => x.QualitySort, o => o.NullSubstitute(QualitySortAlgorithm.Top));

        CreateMap<ServiceConfigYaml, ServiceConfiguration>()
            .ForMember(x => x.InstanceName, o => o.Ignore());

        CreateMap<RadarrConfigYaml, RadarrConfiguration>()
            .IncludeBase<ServiceConfigYaml, ServiceConfiguration>();

        CreateMap<SonarrConfigYaml, SonarrConfiguration>()
            .IncludeBase<ServiceConfigYaml, ServiceConfiguration>();
    }
}
