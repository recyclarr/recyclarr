using TrashLib.Services.Common.QualityDefinition;

namespace TrashLib.Services.Sonarr.QualityDefinition;

public record SonarrQualityData(
    string TrashId,
    string Type,
    IReadOnlyCollection<QualityItem> Qualities
);
