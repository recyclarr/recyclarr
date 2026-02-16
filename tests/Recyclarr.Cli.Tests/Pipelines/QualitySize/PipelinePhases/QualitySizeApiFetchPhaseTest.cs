using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Cli.Pipelines.QualitySize;
using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.ServarrApi.QualityDefinition;

namespace Recyclarr.Cli.Tests.Pipelines.QualitySize.PipelinePhases;

internal sealed class QualitySizeApiFetchPhaseTest
{
    private static PipelinePlan CreatePlan(PlannedQualitySizes qualitySizes)
    {
        return new PipelinePlan { QualitySizes = qualitySizes };
    }

    [Test, AutoMockData]
    public async Task Reset_called_when_reset_before_sync_is_true(
        [Frozen] IQualityDefinitionApiService api,
        QualitySizeApiFetchPhase sut
    )
    {
        var context = new QualitySizePipelineContext
        {
            Plan = CreatePlan(NewPlan.Qs("test", null, true, NewPlan.QsItem("q1", 0, 100, 50))),
        };

        await sut.Execute(context, CancellationToken.None);

        await api.Received(1).ResetQualityDefinitions(Arg.Any<CancellationToken>());
    }

    [Test, AutoMockData]
    public async Task Reset_not_called_when_reset_before_sync_is_false(
        [Frozen] IQualityDefinitionApiService api,
        QualitySizeApiFetchPhase sut
    )
    {
        var context = new QualitySizePipelineContext
        {
            Plan = CreatePlan(NewPlan.Qs("test", null, false, NewPlan.QsItem("q1", 0, 100, 50))),
        };

        await sut.Execute(context, CancellationToken.None);

        await api.DidNotReceive().ResetQualityDefinitions(Arg.Any<CancellationToken>());
    }
}
