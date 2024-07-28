using NSubstitute.ReturnsExtensions;
using Recyclarr.Cli.Pipelines.QualitySize;
using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;
using Recyclarr.Config.Models;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Tests.Pipelines.QualitySize.PipelinePhases;

[TestFixture]
public class QualitySizeConfigPhaseTest
{
    [Test, AutoMockData]
    public void Do_nothing_if_no_quality_definition(
        [Frozen] IServiceConfiguration config,
        QualitySizeConfigPhase sut)
    {
        var context = new QualitySizePipelineContext();
        config.QualityDefinition.ReturnsNull();

        sut.Execute(context);

        context.ConfigOutput.Should().BeNull();
    }

    [Test, AutoMockData]
    public void Do_nothing_if_no_matching_quality_definition(
        [Frozen] IQualitySizeGuideService guide,
        [Frozen] IServiceConfiguration config,
        QualitySizeConfigPhase sut)
    {
        config.QualityDefinition.Returns(new QualityDefinitionConfig {Type = "not_real"});

        guide.GetQualitySizeData(default!).ReturnsForAnyArgs(new[]
        {
            new QualitySizeData {Type = "real"}
        });

        var context = new QualitySizePipelineContext();

        sut.Execute(context);

        context.ConfigOutput.Should().BeNull();
    }

    [Test]
    [InlineAutoMockData("-0.1", "0")]
    [InlineAutoMockData("1.1", "1")]
    public void Preferred_ratio_clamping_works(
        string testPreferred,
        string expectedPreferred,
        [Frozen] IQualitySizeGuideService guide,
        [Frozen] IServiceConfiguration config,
        QualitySizeConfigPhase sut)
    {
        config.QualityDefinition.Returns(new QualityDefinitionConfig
        {
            Type = "real",
            PreferredRatio = decimal.Parse(testPreferred)
        });

        guide.GetQualitySizeData(default!).ReturnsForAnyArgs(new[]
        {
            new QualitySizeData {Type = "real"}
        });

        var context = new QualitySizePipelineContext();

        sut.Execute(context);

        config.QualityDefinition.Should().NotBeNull();
        config.QualityDefinition!.PreferredRatio.Should().Be(decimal.Parse(expectedPreferred));
    }

    [Test, AutoMockData]
    public void Preferred_is_set_via_ratio(
        [Frozen] IQualitySizeGuideService guide,
        [Frozen] IServiceConfiguration config,
        QualitySizeConfigPhase sut)
    {
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
                    new QualityItemWithPreferred("quality1", 0, 100, 90)
                }
            }
        });

        var context = new QualitySizePipelineContext();

        sut.Execute(context);

        context.ConfigOutput.Should().NotBeNull();
        context.ConfigOutput!.Qualities.Should().BeEquivalentTo(new[]
            {
                new QualityItemWithPreferred("quality1", 0, 100, 50)
            },
            o => o
                .Including(x => x.Quality)
                .Including(x => x.Min)
                .Including(x => x.Max)
                .Including(x => x.Preferred));
    }

    [Test, AutoMockData]
    public void Preferred_is_set_via_guide(
        [Frozen] IQualitySizeGuideService guide,
        [Frozen] IServiceConfiguration config,
        QualitySizeConfigPhase sut)
    {
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
                    new QualityItemWithPreferred("quality1", 0, 100, 90)
                }
            }
        });

        var context = new QualitySizePipelineContext();

        sut.Execute(context);

        context.ConfigOutput.Should().NotBeNull();
        context.ConfigOutput!.Qualities.Should().BeEquivalentTo(new[]
            {
                new QualityItemWithPreferred("quality1", 0, 100, 90)
            },
            o => o
                .Including(x => x.Quality)
                .Including(x => x.Min)
                .Including(x => x.Max)
                .Including(x => x.Preferred));
    }
}
