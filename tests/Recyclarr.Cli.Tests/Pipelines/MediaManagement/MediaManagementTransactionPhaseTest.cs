using Recyclarr.Cli.Pipelines;
using Recyclarr.Cli.Pipelines.MediaManagement;
using Recyclarr.Cli.Pipelines.MediaManagement.PipelinePhases;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Config.Models;
using Recyclarr.Servarr.MediaManagement;

namespace Recyclarr.Cli.Tests.Pipelines.MediaManagement;

internal sealed class MediaManagementTransactionPhaseTest
{
    private static TestPlan CreatePlan(PropersAndRepacksMode mode)
    {
        var plan = new TestPlan { MediaManagement = new PlannedMediaManagement(mode) };
        return plan;
    }

    [Test, AutoMockData]
    public async Task Applies_planned_value_to_fetched_data(MediaManagementTransactionPhase sut)
    {
        var context = new MediaManagementPipelineContext
        {
            ApiFetchOutput = new MediaManagementData
            {
                Id = 1,
                PropersAndRepacks = PropersAndRepacksMode.PreferAndUpgrade,
            },
            Plan = CreatePlan(PropersAndRepacksMode.DoNotUpgrade),
        };

        await sut.Execute(context, CancellationToken.None);

        context.TransactionOutput.PropersAndRepacks.Should().Be(PropersAndRepacksMode.DoNotUpgrade);
        context.TransactionOutput.Id.Should().Be(1);
    }

    [Test, AutoMockData]
    public async Task Returns_continue_flow(MediaManagementTransactionPhase sut)
    {
        var context = new MediaManagementPipelineContext
        {
            ApiFetchOutput = new MediaManagementData
            {
                PropersAndRepacks = PropersAndRepacksMode.PreferAndUpgrade,
            },
            Plan = CreatePlan(PropersAndRepacksMode.DoNotUpgrade),
        };

        var result = await sut.Execute(context, CancellationToken.None);

        result.Should().Be(PipelineFlow.Continue);
    }
}
