namespace TrashLib.Services.Radarr.QualityDefinition;

public record RadarrQualityData(
    // ReSharper disable once NotAccessedPositionalProperty.Global
    string TrashId,
    string Type,
    IReadOnlyCollection<RadarrQualityItem> Qualities
);
