namespace Recyclarr.TrashLib.Services.QualitySize;

public record QualitySizeData(
    // ReSharper disable once NotAccessedPositionalProperty.Global
    string TrashId,
    string Type,
    IReadOnlyCollection<QualitySizeItem> Qualities
);
