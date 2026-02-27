namespace Recyclarr.Servarr.QualitySize;

public record QualityDefinitionItem
{
    public int Id { get; init; }
    public required string QualityName { get; init; }
    public decimal MinSize { get; init; }
    public decimal? MaxSize { get; init; }
    public decimal? PreferredSize { get; init; }
}
