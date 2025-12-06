using Recyclarr.ServarrApi.QualityDefinition;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Pipelines.QualitySize;

internal record UpdatedQualityItem
{
    public required string Quality { get; init; }
    public required decimal Min { get; init; }
    public required decimal Max { get; init; }
    public required decimal Preferred { get; init; }
    public required bool IsDifferent { get; init; }
    public required ServiceQualityDefinitionItem ServerItem { get; init; }

    public ServiceQualityDefinitionItem BuildApiItem(QualityItemLimits limits)
    {
        return ServerItem with
        {
            MinSize = Min,
            MaxSize = Max < limits.MaxLimit ? Max : null,
            PreferredSize = Preferred < limits.PreferredLimit ? Preferred : null,
        };
    }
}
