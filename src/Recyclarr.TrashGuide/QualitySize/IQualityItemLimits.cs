namespace Recyclarr.TrashGuide.QualitySize;

public interface IQualityItemLimits
{
    decimal MaxLimit { get; }
    decimal PreferredLimit { get; }
}
