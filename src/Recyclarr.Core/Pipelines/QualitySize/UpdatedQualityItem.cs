using Recyclarr.Servarr.QualitySize;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Pipelines.QualitySize;

public record UpdatedQualityItem
{
    public required string Quality { get; init; }
    public required decimal Min { get; init; }
    public required decimal Max { get; init; }
    public required decimal Preferred { get; init; }
    public required bool IsDifferent { get; init; }
    public required QualityDefinitionItem ServerItem { get; init; }

    public QualityDefinitionItem BuildUpdatedItem(QualityItemLimits limits)
    {
        var size = QualitySizeCalculator.ToServiceSize((Min, Max, Preferred), limits);
        return ServerItem with
        {
            MinSize = size.Min,
            MaxSize = size.Max,
            PreferredSize = size.Preferred,
        };
    }
}
