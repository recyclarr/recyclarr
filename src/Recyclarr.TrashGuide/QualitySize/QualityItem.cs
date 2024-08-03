namespace Recyclarr.TrashGuide.QualitySize;

public record QualityItem(string Quality, decimal Min, decimal Max, decimal Preferred)
{
    public decimal Preferred { get; set; } = Preferred;
}
