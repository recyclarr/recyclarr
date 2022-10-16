using TrashLib.Services.Common.QualityDefinition;

namespace TrashLib.Services.Sonarr.QualityDefinition;

public record SonarrQualityData(
    // ReSharper disable once NotAccessedPositionalProperty.Global
    string TrashId,
    string Type,
    IReadOnlyCollection<QualityItem> Qualities
);
