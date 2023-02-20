using AutoMapper;
using JetBrains.Annotations;
using Recyclarr.TrashLib.Pipelines.ReleaseProfile.Api.Objects;

namespace Recyclarr.TrashLib.Pipelines.ReleaseProfile.Api.Mappings;

[UsedImplicitly]
public class SonarrApiObjectMappingProfile : Profile
{
    public SonarrApiObjectMappingProfile()
    {
        CreateMap<SonarrReleaseProfileV1, SonarrReleaseProfile>()
            .ForMember(d => d.Ignored, x => x.MapFrom(
                s => s.Ignored.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()))
            .ForMember(d => d.Required, x => x.MapFrom(
                s => s.Required.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()));

        CreateMap<SonarrReleaseProfile, SonarrReleaseProfileV1>()
            .ForMember(d => d.Ignored, x => x.MapFrom(
                s => string.Join(',', s.Ignored)))
            .ForMember(d => d.Required, x => x.MapFrom(
                s => string.Join(',', s.Required)));
    }
}
