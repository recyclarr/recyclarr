using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases.Limits;
using Recyclarr.Tests.TestLibrary;
using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.TestLibrary;

public class TestQualityItemLimitFactory : IQualityItemLimitFactory
{
    public Task<QualityItemLimits> Create(SupportedServices serviceType, CancellationToken ct)
    {
        return Task.FromResult(new QualityItemLimits(
            TestQualityItemLimits.MaxUnlimitedThreshold,
            TestQualityItemLimits.PreferredUnlimitedThreshold));
    }
}
