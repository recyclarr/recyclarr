using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Cli.Pipelines.QualitySize;
using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Tests.Pipelines.QualitySize.PipelinePhases;

internal sealed class QualitySizeTransactionPhaseTest
{
    private static readonly QualityItemLimits DefaultLimits = new(400, 400);

    private static TestPlan CreatePlan(PlannedQualitySizes qualitySizes)
    {
        return new TestPlan { QualitySizes = qualitySizes };
    }

    [Test, AutoMockData]
    public async Task Skip_guide_qualities_that_do_not_exist_in_service(
        QualitySizeTransactionPhase sut
    )
    {
        var context = new QualitySizePipelineContext
        {
            Plan = CreatePlan(
                NewPlan.Qs(
                    "test",
                    NewPlan.QsItem("non_existent1", 0, 2, 1),
                    NewPlan.QsItem("non_existent2", 0, 2, 1)
                )
            ),
            Limits = DefaultLimits,
            ApiFetchOutput = [NewQualityDefinition.Item("exists")],
        };

        await sut.Execute(context, CancellationToken.None);

        context.TransactionOutput.Should().BeEmpty();
    }

    [Test, AutoMockData]
    public async Task Mark_qualities_as_not_different_when_they_match_service(
        QualitySizeTransactionPhase sut
    )
    {
        var context = new QualitySizePipelineContext
        {
            Plan = CreatePlan(
                NewPlan.Qs(
                    "test",
                    NewPlan.QsItem("same1", 0, 2, 1),
                    NewPlan.QsItem("same2", 0, 2, 1)
                )
            ),
            Limits = DefaultLimits,
            ApiFetchOutput =
            [
                NewQualityDefinition.Item("same1", maxSize: 2, preferredSize: 1),
                NewQualityDefinition.Item("same2", maxSize: 2, preferredSize: 1),
            ],
        };

        await sut.Execute(context, CancellationToken.None);

        context.TransactionOutput.Should().HaveCount(2);
        context.TransactionOutput.Should().AllSatisfy(x => x.IsDifferent.Should().BeFalse());
    }

    [Test, AutoMockData]
    public async Task Mark_and_prepare_qualities_that_differ_from_service(
        QualitySizeTransactionPhase sut
    )
    {
        var context = new QualitySizePipelineContext
        {
            Plan = CreatePlan(
                NewPlan.Qs(
                    "test",
                    NewPlan.QsItem("same1", 0, 2, 1),
                    NewPlan.QsItem("different1", 0, 3, 1)
                )
            ),
            Limits = DefaultLimits,
            ApiFetchOutput =
            [
                NewQualityDefinition.Item("same1", maxSize: 2, preferredSize: 1),
                NewQualityDefinition.Item("different1", maxSize: 2, preferredSize: 1),
            ],
        };

        await sut.Execute(context, CancellationToken.None);

        context.TransactionOutput.Should().HaveCount(2);

        var differentItems = context.TransactionOutput.Where(x => x.IsDifferent).ToList();
        differentItems.Should().ContainSingle();
        differentItems[0]
            .BuildUpdatedItem(context.Limits)
            .Should()
            .BeEquivalentTo(NewQualityDefinition.Item("different1", maxSize: 3, preferredSize: 1));
    }

    [Test, AutoMockData]
    public async Task Unlimited_values_resolve_to_null_in_server_item(
        QualitySizeTransactionPhase sut
    )
    {
        var limits = new QualityItemLimits(100, 100);
        var context = new QualitySizePipelineContext
        {
            Plan = CreatePlan(NewPlan.Qs("test", NewPlan.QsItem("quality1", 0, null, null))),
            Limits = limits,
            ApiFetchOutput =
            [
                NewQualityDefinition.Item("quality1", maxSize: 50, preferredSize: 50),
            ],
        };

        await sut.Execute(context, CancellationToken.None);

        var item = context.TransactionOutput.Should().ContainSingle().Which;
        item.IsDifferent.Should().BeTrue();
        item.BuildUpdatedItem(limits)
            .Should()
            .BeEquivalentTo(NewQualityDefinition.Item("quality1"));
    }
}
