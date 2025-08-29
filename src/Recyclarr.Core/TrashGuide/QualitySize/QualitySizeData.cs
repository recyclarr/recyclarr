namespace Recyclarr.TrashGuide.QualitySize;

public record QualitySizeData
{
    public string Type { get; init; } = "";
    public IReadOnlyCollection<QualityItem> Qualities { get; init; } = [];
}
