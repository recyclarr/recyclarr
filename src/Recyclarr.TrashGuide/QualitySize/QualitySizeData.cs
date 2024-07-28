namespace Recyclarr.TrashGuide.QualitySize;

public record QualitySizeData
{
    public string Type { get; init; } = "";
    public IReadOnlyCollection<QualityItemWithPreferred> Qualities { get; init; } =
        Array.Empty<QualityItemWithPreferred>();
}
