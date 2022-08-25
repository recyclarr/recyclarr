namespace TrashLib.Services.Radarr.QualityDefinition;

public record RadarrQualityData(
    string TrashId,
    string Type,
    IReadOnlyCollection<RadarrQualityItem> Qualities
);
