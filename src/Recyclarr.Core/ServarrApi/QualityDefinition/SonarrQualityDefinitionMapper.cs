using Recyclarr.Servarr.QualitySize;
using Riok.Mapperly.Abstractions;
using SonarrApi = Recyclarr.Api.Sonarr;

namespace Recyclarr.ServarrApi.QualityDefinition;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
internal static partial class SonarrQualityDefinitionMapper
{
    [MapProperty(
        [nameof(SonarrApi.QualityDefinitionResource.Quality), nameof(SonarrApi.Quality.Name)],
        [nameof(QualityDefinitionItem.QualityName)]
    )]
    public static partial QualityDefinitionItem ToDomain(SonarrApi.QualityDefinitionResource dto);

    public static partial void UpdateDto(
        QualityDefinitionItem domain,
        [MappingTarget] SonarrApi.QualityDefinitionResource dto
    );
}
