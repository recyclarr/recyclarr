using Autofac.Features.Indexed;
using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases.Limits;

public class QualityItemLimitFactory(
    IIndex<SupportedServices, IQualityItemLimitFetcher> limitFactory
) : IQualityItemLimitFactory
{
    public async Task<QualityItemLimits> Create(SupportedServices serviceType, CancellationToken ct)
    {
        if (!limitFactory.TryGetValue(serviceType, out var limitFetcher))
        {
            throw new ArgumentOutOfRangeException(
                nameof(serviceType),
                serviceType,
                "No quality item limits defined for this service type"
            );
        }

        return await limitFetcher.GetLimits(ct);
    }
}
