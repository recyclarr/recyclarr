using Recyclarr.Cli.Pipelines.QualitySize;
using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;
using Recyclarr.Core.TestLibrary;
using Recyclarr.ServarrApi.QualityDefinition;

namespace Recyclarr.Cli.Tests.Pipelines.QualitySize.PipelinePhases;

internal sealed class QualitySizeTransactionPhaseTest
{
    [Test, AutoMockData]
    public async Task Skip_guide_qualities_that_do_not_exist_in_service(
        QualitySizeTransactionPhase sut
    )
    {
        var context = new QualitySizePipelineContext
        {
            Qualities =
            [
                NewQualitySize.WithLimits("non_existent1", 0, 2, 1),
                NewQualitySize.WithLimits("non_existent2", 0, 2, 1),
            ],
            ApiFetchOutput = new List<ServiceQualityDefinitionItem>
            {
                new() { Quality = new ServiceQualityItem { Name = "exists" } },
            },
        };

        await sut.Execute(context, CancellationToken.None);

        context.TransactionOutput.Should().BeEmpty();
    }

    [Test, AutoMockData]
    public async Task Skip_guide_qualities_that_are_not_different_from_service(
        QualitySizeTransactionPhase sut
    )
    {
        var context = new QualitySizePipelineContext
        {
            Qualities =
            [
                NewQualitySize.WithLimits("same1", 0, 2, 1),
                NewQualitySize.WithLimits("same2", 0, 2, 1),
            ],
            ApiFetchOutput = new List<ServiceQualityDefinitionItem>
            {
                new()
                {
                    Quality = new ServiceQualityItem { Name = "same1" },
                    MinSize = 0,
                    MaxSize = 2,
                    PreferredSize = 1,
                },
                new()
                {
                    Quality = new ServiceQualityItem { Name = "same2" },
                    MinSize = 0,
                    MaxSize = 2,
                    PreferredSize = 1,
                },
            },
        };

        await sut.Execute(context, CancellationToken.None);

        context.TransactionOutput.Should().BeEmpty();
    }

    [Test, AutoMockData]
    public async Task Sync_guide_qualities_that_are_different_from_service(
        QualitySizeTransactionPhase sut
    )
    {
        var context = new QualitySizePipelineContext
        {
            Qualities =
            [
                NewQualitySize.WithLimits("same1", 0, 2, 1),
                NewQualitySize.WithLimits("different1", 0, 3, 1),
            ],
            ApiFetchOutput = new List<ServiceQualityDefinitionItem>
            {
                new()
                {
                    Quality = new ServiceQualityItem { Name = "same1" },
                    MinSize = 0,
                    MaxSize = 2,
                    PreferredSize = 1,
                },
                new()
                {
                    Quality = new ServiceQualityItem { Name = "different1" },
                    MinSize = 0,
                    MaxSize = 2,
                    PreferredSize = 1,
                },
            },
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(
                new List<ServiceQualityDefinitionItem>
                {
                    new()
                    {
                        Quality = new ServiceQualityItem { Name = "different1" },
                        MinSize = 0,
                        MaxSize = 3,
                        PreferredSize = 1,
                    },
                }
            );
    }
}
