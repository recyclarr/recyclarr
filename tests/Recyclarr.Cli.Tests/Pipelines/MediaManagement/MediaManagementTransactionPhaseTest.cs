using Recyclarr.Cli.Pipelines;
using Recyclarr.Cli.Pipelines.MediaManagement;
using Recyclarr.Cli.Pipelines.MediaManagement.PipelinePhases;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.MediaManagement;

namespace Recyclarr.Cli.Tests.Pipelines.MediaManagement;

internal sealed class MediaManagementTransactionPhaseTest
{
    private static TestPlan CreatePlan(PropersAndRepacksMode mode)
    {
        var plan = new TestPlan { MediaManagement = new PlannedMediaManagement(mode) };
        return plan;
    }

    [Test, AutoMockData]
    public async Task Applies_planned_value_to_fetched_dto(MediaManagementTransactionPhase sut)
    {
        var context = new MediaManagementPipelineContext
        {
            ApiFetchOutput = new MediaManagementDto
            {
                Id = 1,
                DownloadPropersAndRepacks = PropersAndRepacksMode.PreferAndUpgrade,
            },
            Plan = CreatePlan(PropersAndRepacksMode.DoNotUpgrade),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.DownloadPropersAndRepacks.Should()
            .Be(PropersAndRepacksMode.DoNotUpgrade);
        context.TransactionOutput.Id.Should().Be(1);
    }

    [Test, AutoMockData]
    public async Task Preserves_extra_json_from_api_fetch(MediaManagementTransactionPhase sut)
    {
        var apiFetchDto = new MediaManagementDto
        {
            Id = 1,
            DownloadPropersAndRepacks = PropersAndRepacksMode.PreferAndUpgrade,
        };
        apiFetchDto.ExtraJson["someOtherProperty"] = "preserved";

        var context = new MediaManagementPipelineContext
        {
            ApiFetchOutput = apiFetchDto,
            Plan = CreatePlan(PropersAndRepacksMode.DoNotPrefer),
        };

        await sut.Execute(context, CancellationToken.None);

        context.TransactionOutput.ExtraJson.Should().ContainKey("someOtherProperty");
    }

    [Test, AutoMockData]
    public async Task Returns_continue_flow(MediaManagementTransactionPhase sut)
    {
        var context = new MediaManagementPipelineContext
        {
            ApiFetchOutput = new MediaManagementDto
            {
                DownloadPropersAndRepacks = PropersAndRepacksMode.PreferAndUpgrade,
            },
            Plan = CreatePlan(PropersAndRepacksMode.DoNotUpgrade),
        };

        var result = await sut.Execute(context, CancellationToken.None);

        result.Should().Be(PipelineFlow.Continue);
    }
}
