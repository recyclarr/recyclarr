using Recyclarr.Cli.Pipelines.QualitySize.Api;
using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Tests.Pipelines.QualitySize.PipelinePhases;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class QualitySizeTransactionPhaseTest
{
    [Test, AutoMockData]
    public void Skip_guide_qualities_that_do_not_exist_in_service(
        QualitySizeTransactionPhase sut)
    {
        var guideData = new[]
        {
            new QualitySizeItem("non_existent1", 0, 2, 1),
            new QualitySizeItem("non_existent2", 0, 2, 1)
        };

        var serviceData = new List<ServiceQualityDefinitionItem>
        {
            new()
            {
                Quality = new ServiceQualityItem {Name = "exists"}
            }
        };

        var result = sut.Execute(guideData, serviceData);

        result.Should().BeEmpty();
    }

    [Test, AutoMockData]
    public void Skip_guide_qualities_that_are_not_different_from_service(
        QualitySizeTransactionPhase sut)
    {
        var guideData = new[]
        {
            new QualitySizeItem("same1", 0, 2, 1),
            new QualitySizeItem("same2", 0, 2, 1)
        };

        var serviceData = new List<ServiceQualityDefinitionItem>
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
        };

        var result = sut.Execute(guideData, serviceData);

        result.Should().BeEmpty();
    }

    [Test, AutoMockData]
    public void Sync_guide_qualities_that_are_different_from_service(
        QualitySizeTransactionPhase sut)
    {
        var guideData = new[]
        {
            new QualitySizeItem("same1", 0, 2, 1),
            new QualitySizeItem("different1", 0, 3, 1)
        };

        var serviceData = new List<ServiceQualityDefinitionItem>
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
        };

        var result = sut.Execute(guideData, serviceData);

        result.Should().BeEquivalentTo(new List<ServiceQualityDefinitionItem>
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
