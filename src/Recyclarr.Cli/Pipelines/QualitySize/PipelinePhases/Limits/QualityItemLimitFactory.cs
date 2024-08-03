using Autofac.Features.Indexed;
using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases.Limits;

public class QualityItemLimitFactory(IIndex<SupportedServices, IQualityItemLimits> limitFactory)
{
    public QualityItemWithLimits Create(QualityItem item, SupportedServices serviceType)
    {
        if (!limitFactory.TryGetValue(serviceType, out var limits))
        {
            throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType,
                "No quality item limits defined for this service type");
        }

        return new QualityItemWithLimits(item, limits);
    }
}
