namespace Recyclarr.TrashGuide.QualitySize;

public record QualityItem
{
    public required string Quality { get; init; }
    public required decimal Min { get; set; }
    public required decimal Max { get; set; }
    public required decimal Preferred { get; set; }
}
