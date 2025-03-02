using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases.Limits;
using Recyclarr.Core.TestLibrary;
using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Tests.Reusable;

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Created by AutoFixture"
)]
internal sealed class TestQualityItemLimitFactory : IQualityItemLimitFactory
{
    public Task<QualityItemLimits> Create(SupportedServices serviceType, CancellationToken ct)
    {
        return Task.FromResult(
            new QualityItemLimits(
                TestQualityItemLimits.MaxUnlimitedThreshold,
                TestQualityItemLimits.PreferredUnlimitedThreshold
            )
        );
    }
}
