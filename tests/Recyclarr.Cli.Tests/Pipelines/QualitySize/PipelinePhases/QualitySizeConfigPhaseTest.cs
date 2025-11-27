using System.Globalization;
using System.IO.Abstractions;
using System.Text.Json;
using NSubstitute.ReturnsExtensions;
using Recyclarr.Cli.Pipelines.QualitySize;
using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;
using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases.Limits;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Config.Models;
using Recyclarr.Json;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Tests.Pipelines.QualitySize.PipelinePhases;

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
        [Frozen] IServiceConfiguration config,
        ILogger log,
        IQualityItemLimitFactory limitFactory
    )
    {
        config.QualityDefinition.Returns(new QualityDefinitionConfig { Type = "not_real" });
        config.ServiceType.Returns(SupportedServices.Radarr);

        var guide = CreateQualityQuery(
            [new RadarrQualitySizeResource { Type = "real" }],
            [new SonarrQualitySizeResource { Type = "real" }]
        );

        var sut = new QualitySizeConfigPhase(log, guide, config, limitFactory);
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
        [Frozen] IServiceConfiguration config,
        ILogger log,
        IQualityItemLimitFactory limitFactory
    )
    {
        config.QualityDefinition.Returns(
            new QualityDefinitionConfig
            {
                Type = "real",
                PreferredRatio = decimal.Parse(testPreferred, CultureInfo.InvariantCulture),
            }
        );
        config.ServiceType.Returns(SupportedServices.Radarr);

        var guide = CreateQualityQuery(
            [new RadarrQualitySizeResource { Type = "real" }],
            [new SonarrQualitySizeResource { Type = "real" }]
        );

        var sut = new QualitySizeConfigPhase(log, guide, config, limitFactory);
        var context = new QualitySizePipelineContext();

        await sut.Execute(context, CancellationToken.None);

        config.QualityDefinition.Should().NotBeNull();
        config
            .QualityDefinition!.PreferredRatio.Should()
            .Be(decimal.Parse(expectedPreferred, CultureInfo.InvariantCulture));
    }

    [Test, AutoMockData]
    public async Task Preferred_is_set_via_ratio(
        [Frozen] IServiceConfiguration config,
        [Frozen(Matching.ImplementedInterfaces)] TestQualityItemLimitFactory limitFactory,
        ILogger log
    )
    {
        config.QualityDefinition.Returns(
            new QualityDefinitionConfig { Type = "real", PreferredRatio = 0.5m }
        );
        config.ServiceType.Returns(SupportedServices.Radarr);

        var guide = CreateQualityQuery(
            [
                new RadarrQualitySizeResource
                {
                    Type = "real",
                    Qualities = [new QualityItem("quality1", 0, 100, 90)],
                },
            ],
            []
        );

        var sut = new QualitySizeConfigPhase(log, guide, config, limitFactory);
        var context = new QualitySizePipelineContext();

        await sut.Execute(context, CancellationToken.None);

        context
            .Qualities.Select(x => x.Item)
            .Should()
            .BeEquivalentTo([new QualityItem("quality1", 0, 100, 50)]);
    }

    [Test, AutoMockData]
    public async Task Preferred_is_set_via_guide(
        [Frozen] IServiceConfiguration config,
        [Frozen(Matching.ImplementedInterfaces)] TestQualityItemLimitFactory limitFactory,
        ILogger log
    )
    {
        config.QualityDefinition.Returns(new QualityDefinitionConfig { Type = "real" });
        config.ServiceType.Returns(SupportedServices.Radarr);

        var guide = CreateQualityQuery(
            [
                new RadarrQualitySizeResource
                {
                    Type = "real",
                    Qualities = [new QualityItem("quality1", 0, 100, 90)],
                },
            ],
            []
        );

        var sut = new QualitySizeConfigPhase(log, guide, config, limitFactory);
        var context = new QualitySizePipelineContext();

        await sut.Execute(context, CancellationToken.None);

        context
            .Qualities.Select(x => x.Item)
            .Should()
            .BeEquivalentTo([new QualityItem("quality1", 0, 100, 90)]);
    }

    private static QualitySizeResourceQuery CreateQualityQuery(
        IReadOnlyCollection<RadarrQualitySizeResource> radarrData,
        IReadOnlyCollection<SonarrQualitySizeResource> sonarrData
    )
    {
        var fs = new MockFileSystem();
        var registry = new ResourceRegistry<IFileInfo>();

        var radarrFiles = radarrData
            .Select(
                (resource, i) =>
                {
                    var path = $"/radarr/quality{i}.json";
                    fs.AddFile(path, new MockFileData(SerializeResource(resource)));
                    return fs.FileInfo.New(path);
                }
            )
            .ToList();

        var sonarrFiles = sonarrData
            .Select(
                (resource, i) =>
                {
                    var path = $"/sonarr/quality{i}.json";
                    fs.AddFile(path, new MockFileData(SerializeResource(resource)));
                    return fs.FileInfo.New(path);
                }
            )
            .ToList();

        registry.Register<RadarrQualitySizeResource>(radarrFiles);
        registry.Register<SonarrQualitySizeResource>(sonarrFiles);

        var loader = new JsonResourceLoader(fs);
        return new QualitySizeResourceQuery(registry, loader);
    }

    private static string SerializeResource(QualitySizeResource resource)
    {
        return JsonSerializer.Serialize(resource, GlobalJsonSerializerSettings.Guide);
    }
}
