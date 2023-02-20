using NSubstitute.ReturnsExtensions;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Pipelines.QualitySize;
using Recyclarr.TrashLib.Pipelines.QualitySize.Guide;
using Recyclarr.TrashLib.Pipelines.QualitySize.PipelinePhases;

namespace Recyclarr.TrashLib.Tests.Pipelines.QualitySize.PipelinePhases;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class QualitySizeGuidePhaseTest
{
    [Test, AutoMockData]
    public void Do_nothing_if_no_quality_definition(QualitySizeGuidePhase sut)
    {
        var config = Substitute.For<IServiceConfiguration>();
        config.QualityDefinition.ReturnsNull();

        var result = sut.Execute(config);

        result.Should().BeNull();
    }

    [Test, AutoMockData]
    public void Do_nothing_if_no_matching_quality_definition(
        [Frozen] IQualityGuideService guide,
        QualitySizeGuidePhase sut)
    {
        var config = Substitute.For<IServiceConfiguration>();
        config.QualityDefinition.Returns(new QualityDefinitionConfig {Type = "not_real"});

        guide.GetQualitySizeData(default!).ReturnsForAnyArgs(new[]
        {
            new QualitySizeData {Type = "real"}
        });

        var result = sut.Execute(config);

        result.Should().BeNull();
    }

    [Test]
    [InlineAutoMockData("-0.1", "0")]
    [InlineAutoMockData("1.1", "1")]
    public void Preferred_ratio_clamping_works(
        string testPreferred,
        string expectedPreferred,
        [Frozen] IQualityGuideService guide,
        QualitySizeGuidePhase sut)
    {
        var config = Substitute.For<IServiceConfiguration>();
        config.QualityDefinition.Returns(new QualityDefinitionConfig
        {
            Type = "real",
            PreferredRatio = decimal.Parse(testPreferred)
        });

        guide.GetQualitySizeData(default!).ReturnsForAnyArgs(new[]
        {
            new QualitySizeData {Type = "real"}
        });

        _ = sut.Execute(config);

        config.QualityDefinition?.PreferredRatio.Should().Be(decimal.Parse(expectedPreferred));
    }

    [Test, AutoMockData]
    public void Preferred_is_set_via_ratio(
        [Frozen] IQualityGuideService guide,
        QualitySizeGuidePhase sut)
    {
        var config = Substitute.For<IServiceConfiguration>();
        config.QualityDefinition.Returns(new QualityDefinitionConfig
        {
            Type = "real",
            PreferredRatio = 0.5m
        });

        guide.GetQualitySizeData(default!).ReturnsForAnyArgs(new[]
        {
            new QualitySizeData
            {
                Type = "real",
                Qualities = new[]
                {
                    new QualitySizeItem("quality1", 0, 100, 90)
                }
            }
        });

        var result = sut.Execute(config);

        result?.Qualities.Should().BeEquivalentTo(new[]
            {
                new QualitySizeItem("quality1", 0, 100, 50)
            },
            o => o
                .Including(x => x.Quality)
                .Including(x => x.Min)
                .Including(x => x.Max)
                .Including(x => x.Preferred));
    }

    [Test, AutoMockData]
    public void Preferred_is_set_via_guide(
        [Frozen] IQualityGuideService guide,
        QualitySizeGuidePhase sut)
    {
        var config = Substitute.For<IServiceConfiguration>();
        config.QualityDefinition.Returns(new QualityDefinitionConfig
        {
            Type = "real"
        });

        guide.GetQualitySizeData(default!).ReturnsForAnyArgs(new[]
        {
            new QualitySizeData
            {
                Type = "real",
                Qualities = new[]
                {
                    new QualitySizeItem("quality1", 0, 100, 90)
                }
            }
        });

        var result = sut.Execute(config);

        result?.Qualities.Should().BeEquivalentTo(new[]
            {
                new QualitySizeItem("quality1", 0, 100, 90)
            },
            o => o
                .Including(x => x.Quality)
                .Including(x => x.Min)
                .Including(x => x.Max)
                .Including(x => x.Preferred));
    }
}
