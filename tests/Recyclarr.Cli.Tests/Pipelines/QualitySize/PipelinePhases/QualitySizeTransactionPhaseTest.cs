using Recyclarr.Cli.Pipelines.QualitySize;
using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;
using Recyclarr.ServarrApi.QualityDefinition;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Tests.Pipelines.QualitySize.PipelinePhases;

[TestFixture]
public class QualitySizeTransactionPhaseTest
{
    [Test, AutoMockData]
    public void Skip_guide_qualities_that_do_not_exist_in_service(
        QualitySizeTransactionPhase sut)
    {
        var context = new QualitySizePipelineContext
        {
            ConfigOutput = new QualitySizeData
            {
                Qualities = new[]
                {
                    new QualitySizeItem("non_existent1", 0, 2, 1),
                    new QualitySizeItem("non_existent2", 0, 2, 1)
                }
            },
            ApiFetchOutput = new List<ServiceQualityDefinitionItem>
            {
                new()
                {
                    Quality = new ServiceQualityItem {Name = "exists"}
                }
            }
        };

        sut.Execute(context);

        context.TransactionOutput.Should().BeEmpty();
    }

    [Test, AutoMockData]
    public void Skip_guide_qualities_that_are_not_different_from_service(
        QualitySizeTransactionPhase sut)
    {
        var context = new QualitySizePipelineContext
        {
            ConfigOutput = new QualitySizeData
            {
                Qualities = new[]
                {
                    new QualitySizeItem("same1", 0, 2, 1),
                    new QualitySizeItem("same2", 0, 2, 1)
                }
            },
            ApiFetchOutput = new List<ServiceQualityDefinitionItem>
            {
                new()
                {
                    Quality = new ServiceQualityItem {Name = "same1"},
                    MinSize = 0,
                    MaxSize = 2,
                    PreferredSize = 1
                },
                new()
                {
                    Quality = new ServiceQualityItem {Name = "same2"},
                    MinSize = 0,
                    MaxSize = 2,
                    PreferredSize = 1
                }
            }
        };

        sut.Execute(context);

        context.TransactionOutput.Should().BeEmpty();
    }

    [Test, AutoMockData]
    public void Sync_guide_qualities_that_are_different_from_service(
        QualitySizeTransactionPhase sut)
    {
        var context = new QualitySizePipelineContext
        {
            ConfigOutput = new QualitySizeData
            {
                Qualities = new[]
                {
                    new QualitySizeItem("same1", 0, 2, 1),
                    new QualitySizeItem("different1", 0, 3, 1)
                }
            },
            ApiFetchOutput = new List<ServiceQualityDefinitionItem>
            {
                new()
                {
                    Quality = new ServiceQualityItem {Name = "same1"},
                    MinSize = 0,
                    MaxSize = 2,
                    PreferredSize = 1
                },
                new()
                {
                    Quality = new ServiceQualityItem {Name = "different1"},
                    MinSize = 0,
                    MaxSize = 2,
                    PreferredSize = 1
                }
            }
        };

        sut.Execute(context);

        context.TransactionOutput.Should().BeEquivalentTo(new List<ServiceQualityDefinitionItem>
        {
            new()
            {
                Quality = new ServiceQualityItem {Name = "different1"},
                MinSize = 0,
                MaxSize = 3,
                PreferredSize = 1
            }
        });
    }
}
