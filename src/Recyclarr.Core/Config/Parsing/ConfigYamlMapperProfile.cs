using AutoMapper;
using Recyclarr.Config.Models;

namespace Recyclarr.Config.Parsing;

[UsedImplicitly]
public class ConfigYamlMapperProfile : Profile
{
    public ConfigYamlMapperProfile()
    {
        CreateMap<QualityScoreConfigYaml, AssignScoresToConfig>();
        CreateMap<CustomFormatConfigYaml, CustomFormatConfig>();
        CreateMap<QualitySizeConfigYaml, QualityDefinitionConfig>();
        CreateMap<ResetUnmatchedScoresConfigYaml, ResetUnmatchedScoresConfig>();

        CreateMap<RadarrMediaNamingConfigYaml, RadarrMediaNamingConfig>();
        CreateMap<RadarrMovieNamingConfigYaml, RadarrMovieNamingConfig>();

        CreateMap<SonarrMediaNamingConfigYaml, SonarrMediaNamingConfig>();
        CreateMap<SonarrEpisodeNamingConfigYaml, SonarrEpisodeNamingConfig>();

        CreateMap<QualityProfileQualityConfigYaml, QualityProfileQualityConfig>()
            .ForMember(x => x.Enabled, o => o.NullSubstitute(true));

        CreateMap<QualityProfileConfigYaml, QualityProfileConfig>()
            .ForMember(x => x.UpgradeAllowed, o => o.MapFrom(x => x.Upgrade!.Allowed))
            .ForMember(x => x.UpgradeUntilQuality, o => o.MapFrom(x => x.Upgrade!.UntilQuality))
            .ForMember(x => x.UpgradeUntilScore, o => o.MapFrom(x => x.Upgrade!.UntilScore))
            .ForMember(x => x.QualitySort, o => o.NullSubstitute(QualitySortAlgorithm.Top))
            .ForMember(x => x.ResetUnmatchedScores, o => o.UseDestinationValue());

        CreateMap<ServiceConfigYaml, ServiceConfiguration>()
            .ForMember(x => x.InstanceName, o => o.Ignore());

        CreateMap<RadarrConfigYaml, RadarrConfiguration>()
            .IncludeBase<ServiceConfigYaml, ServiceConfiguration>()
            .ForMember(x => x.MediaNaming, o => o.UseDestinationValue());

        CreateMap<SonarrConfigYaml, SonarrConfiguration>()
            .IncludeBase<ServiceConfigYaml, ServiceConfiguration>()
            .ForMember(x => x.MediaNaming, o => o.UseDestinationValue());
    }
}
