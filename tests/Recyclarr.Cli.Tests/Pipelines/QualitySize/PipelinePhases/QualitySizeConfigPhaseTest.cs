using System.Globalization;
using NSubstitute.ReturnsExtensions;
using Recyclarr.Cli.Pipelines.QualitySize;
using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.Config.Models;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Tests.Pipelines.QualitySize.PipelinePhases;

[TestFixture]
internal sealed class QualitySizeConfigPhaseTest
{
    [Test, AutoMockData]
    public async Task Do_nothing_if_no_quality_definition(
        [Frozen] IServiceConfiguration config,
        QualitySizeConfigPhase sut
    )
    {
        var context = new QualitySizePipelineContext();
        config.QualityDefinition.ReturnsNull();

        await sut.Execute(context, CancellationToken.None);

        context.QualitySizeType.Should().BeEmpty();
        context.Qualities.Should().BeEmpty();
    }

    [Test, AutoMockData]
    public async Task Do_nothing_if_no_matching_quality_definition(
        [Frozen] IQualitySizeGuideService guide,
        [Frozen] IServiceConfiguration config,
        QualitySizeConfigPhase sut
    )
    {
        config.QualityDefinition.Returns(new QualityDefinitionConfig { Type = "not_real" });

        guide
            .GetQualitySizeData(default!)
            .ReturnsForAnyArgs([new QualitySizeData { Type = "real" }]);

        var context = new QualitySizePipelineContext();

        await sut.Execute(context, CancellationToken.None);

        context.QualitySizeType.Should().BeEmpty();
        context.Qualities.Should().BeEmpty();
    }

    [Test]
    [InlineAutoMockData("-0.1", "0")]
    [InlineAutoMockData("1.1", "1")]
    public async Task Preferred_ratio_clamping_works(
        string testPreferred,
        string expectedPreferred,
        [Frozen] IQualitySizeGuideService guide,
        [Frozen] IServiceConfiguration config,
        QualitySizeConfigPhase sut
    )
    {
        config.QualityDefinition.Returns(
            new QualityDefinitionConfig
            {
                Type = "real",
                PreferredRatio = decimal.Parse(testPreferred, CultureInfo.InvariantCulture),
            }
        );

        guide
            .GetQualitySizeData(default!)
            .ReturnsForAnyArgs([new QualitySizeData { Type = "real" }]);

        var context = new QualitySizePipelineContext();

        await sut.Execute(context, CancellationToken.None);

        config.QualityDefinition.Should().NotBeNull();
        config
            .QualityDefinition!.PreferredRatio.Should()
            .Be(decimal.Parse(expectedPreferred, CultureInfo.InvariantCulture));
    }

    [Test, AutoMockData]
    public async Task Preferred_is_set_via_ratio(
        [Frozen] IQualitySizeGuideService guide,
        [Frozen] IServiceConfiguration config,
        [Frozen(Matching.ImplementedInterfaces)] TestQualityItemLimitFactory limitFactory,
        QualitySizeConfigPhase sut
    )
    {
        config.QualityDefinition.Returns(
            new QualityDefinitionConfig { Type = "real", PreferredRatio = 0.5m }
        );

        guide
            .GetQualitySizeData(default!)
            .ReturnsForAnyArgs(
                [
                    new QualitySizeData
                    {
                        Type = "real",
                        Qualities = [new QualityItem("quality1", 0, 100, 90)],
                    },
                ]
            );

        var context = new QualitySizePipelineContext();

        await sut.Execute(context, CancellationToken.None);

        context
            .Qualities.Select(x => x.Item)
            .Should()
            .BeEquivalentTo([new QualityItem("quality1", 0, 100, 50)]);
    }

    [Test, AutoMockData]
    public async Task Preferred_is_set_via_guide(
        [Frozen] IQualitySizeGuideService guide,
        [Frozen] IServiceConfiguration config,
        [Frozen(Matching.ImplementedInterfaces)] TestQualityItemLimitFactory limitFactory,
        QualitySizeConfigPhase sut
    )
    {
        config.QualityDefinition.Returns(new QualityDefinitionConfig { Type = "real" });

        guide
            .GetQualitySizeData(default!)
            .ReturnsForAnyArgs(
                [
                    new QualitySizeData
                    {
                        Type = "real",
                        Qualities = [new QualityItem("quality1", 0, 100, 90)],
                    },
                ]
            );

        var context = new QualitySizePipelineContext();

        await sut.Execute(context, CancellationToken.None);

        context
            .Qualities.Select(x => x.Item)
            .Should()
            .BeEquivalentTo([new QualityItem("quality1", 0, 100, 90)]);
    }
}
