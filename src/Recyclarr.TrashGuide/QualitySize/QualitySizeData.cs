namespace Recyclarr.TrashGuide.QualitySize;

public record QualitySizeData
{
    public string Type { get; init; } = "";
    public IReadOnlyCollection<QualitySizeItem> Qualities { get; init; } = Array.Empty<QualitySizeItem>();
}
