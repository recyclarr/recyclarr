using Recyclarr.Servarr.QualitySize;
using Riok.Mapperly.Abstractions;
using RadarrApi = Recyclarr.Api.Radarr;

namespace Recyclarr.ServarrApi.QualityDefinition;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
internal static partial class RadarrQualityDefinitionMapper
{
    [MapProperty(
        [nameof(RadarrApi.QualityDefinitionResource.Quality), nameof(RadarrApi.Quality.Name)],
        [nameof(QualityDefinitionItem.QualityName)]
    )]
    public static partial QualityDefinitionItem ToDomain(RadarrApi.QualityDefinitionResource dto);

    public static partial void UpdateDto(
        QualityDefinitionItem domain,
        [MappingTarget] RadarrApi.QualityDefinitionResource dto
    );
}
