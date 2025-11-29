using System.Globalization;
using System.IO.Abstractions;
using System.Text.Json;
using NSubstitute.ReturnsExtensions;
using Recyclarr.Cli.Pipelines;
using Recyclarr.Cli.Pipelines.QualitySize;
using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;
using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases.Limits;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Json;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.TrashGuide;

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
                    Qualities = [NewQualitySize.Item("quality1", 0, 100, 90)],
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
            .BeEquivalentTo([NewQualitySize.Item("quality1", 0, 100, 50)]);
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
                    Qualities = [NewQualitySize.Item("quality1", 0, 100, 90)],
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
            .BeEquivalentTo([NewQualitySize.Item("quality1", 0, 100, 90)]);
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
        var log = Substitute.For<ILogger>();
        return new QualitySizeResourceQuery(registry, loader, log);
    }

    private static string SerializeResource(QualitySizeResource resource)
    {
        return JsonSerializer.Serialize(resource, GlobalJsonSerializerSettings.Guide);
    }

    [Test, AutoMockData]
    public async Task Per_quality_overrides_are_applied(
        [Frozen] IServiceConfiguration config,
        [Frozen(Matching.ImplementedInterfaces)] TestQualityItemLimitFactory limitFactory,
        ILogger log
    )
    {
        config.QualityDefinition.Returns(
            new QualityDefinitionConfig
            {
                Type = "real",
                Qualities =
                [
                    new QualityDefinitionItemConfig
                    {
                        Name = "quality1",
                        Min = new QualitySizeValue.Numeric(5),
                        Max = new QualitySizeValue.Numeric(80),
                        Preferred = new QualitySizeValue.Numeric(50),
                    },
                ],
            }
        );
        config.ServiceType.Returns(SupportedServices.Radarr);

        var guide = CreateQualityQuery(
            [
                new RadarrQualitySizeResource
                {
                    Type = "real",
                    Qualities = [NewQualitySize.Item("quality1", 0, 100, 90)],
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
            .BeEquivalentTo([NewQualitySize.Item("quality1", 5, 80, 50)]);
    }

    [Test, AutoMockData]
    public async Task Unspecified_qualities_keep_guide_defaults(
        [Frozen] IServiceConfiguration config,
        [Frozen(Matching.ImplementedInterfaces)] TestQualityItemLimitFactory limitFactory,
        ILogger log
    )
    {
        config.QualityDefinition.Returns(
            new QualityDefinitionConfig
            {
                Type = "real",
                Qualities =
                [
                    new QualityDefinitionItemConfig
                    {
                        Name = "quality1",
                        Min = new QualitySizeValue.Numeric(10),
                    },
                ],
            }
        );
        config.ServiceType.Returns(SupportedServices.Radarr);

        var guide = CreateQualityQuery(
            [
                new RadarrQualitySizeResource
                {
                    Type = "real",
                    Qualities =
                    [
                        NewQualitySize.Item("quality1", 0, 100, 90),
                        NewQualitySize.Item("quality2", 5, 50, 25),
                    ],
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
            .BeEquivalentTo([
                NewQualitySize.Item("quality1", 10, 100, 90),
                NewQualitySize.Item("quality2", 5, 50, 25),
            ]);
    }

    [Test, AutoMockData]
    public async Task Invalid_quality_name_returns_terminate(
        [Frozen] IServiceConfiguration config,
        [Frozen(Matching.ImplementedInterfaces)] TestQualityItemLimitFactory limitFactory,
        ILogger log
    )
    {
        config.QualityDefinition.Returns(
            new QualityDefinitionConfig
            {
                Type = "real",
                Qualities =
                [
                    new QualityDefinitionItemConfig
                    {
                        Name = "nonexistent_quality",
                        Min = new QualitySizeValue.Numeric(10),
                    },
                ],
            }
        );
        config.ServiceType.Returns(SupportedServices.Radarr);

        var guide = CreateQualityQuery(
            [
                new RadarrQualitySizeResource
                {
                    Type = "real",
                    Qualities = [NewQualitySize.Item("quality1", 0, 100, 90)],
                },
            ],
            []
        );

        var sut = new QualitySizeConfigPhase(log, guide, config, limitFactory);
        var context = new QualitySizePipelineContext();

        var result = await sut.Execute(context, CancellationToken.None);

        result.Should().Be(PipelineFlow.Terminate);
    }

    [Test, AutoMockData]
    public async Task Min_greater_than_preferred_returns_terminate(
        [Frozen] IServiceConfiguration config,
        [Frozen(Matching.ImplementedInterfaces)] TestQualityItemLimitFactory limitFactory,
        ILogger log
    )
    {
        config.QualityDefinition.Returns(
            new QualityDefinitionConfig
            {
                Type = "real",
                Qualities =
                [
                    new QualityDefinitionItemConfig
                    {
                        Name = "quality1",
                        Min = new QualitySizeValue.Numeric(60),
                        Preferred = new QualitySizeValue.Numeric(50),
                    },
                ],
            }
        );
        config.ServiceType.Returns(SupportedServices.Radarr);

        var guide = CreateQualityQuery(
            [
                new RadarrQualitySizeResource
                {
                    Type = "real",
                    Qualities = [NewQualitySize.Item("quality1", 0, 100, 90)],
                },
            ],
            []
        );

        var sut = new QualitySizeConfigPhase(log, guide, config, limitFactory);
        var context = new QualitySizePipelineContext();

        var result = await sut.Execute(context, CancellationToken.None);

        result.Should().Be(PipelineFlow.Terminate);
    }

    [Test, AutoMockData]
    public async Task Preferred_greater_than_max_returns_terminate(
        [Frozen] IServiceConfiguration config,
        [Frozen(Matching.ImplementedInterfaces)] TestQualityItemLimitFactory limitFactory,
        ILogger log
    )
    {
        config.QualityDefinition.Returns(
            new QualityDefinitionConfig
            {
                Type = "real",
                Qualities =
                [
                    new QualityDefinitionItemConfig
                    {
                        Name = "quality1",
                        Preferred = new QualitySizeValue.Numeric(80),
                        Max = new QualitySizeValue.Numeric(50),
                    },
                ],
            }
        );
        config.ServiceType.Returns(SupportedServices.Radarr);

        var guide = CreateQualityQuery(
            [
                new RadarrQualitySizeResource
                {
                    Type = "real",
                    Qualities = [NewQualitySize.Item("quality1", 0, 100, 90)],
                },
            ],
            []
        );

        var sut = new QualitySizeConfigPhase(log, guide, config, limitFactory);
        var context = new QualitySizePipelineContext();

        var result = await sut.Execute(context, CancellationToken.None);

        result.Should().Be(PipelineFlow.Terminate);
    }

    [Test, AutoMockData]
    public async Task Unlimited_value_is_converted_to_limit(
        [Frozen] IServiceConfiguration config,
        [Frozen(Matching.ImplementedInterfaces)] TestQualityItemLimitFactory limitFactory,
        ILogger log
    )
    {
        config.QualityDefinition.Returns(
            new QualityDefinitionConfig
            {
                Type = "real",
                Qualities =
                [
                    new QualityDefinitionItemConfig
                    {
                        Name = "quality1",
                        Max = new QualitySizeValue.Unlimited(),
                        Preferred = new QualitySizeValue.Unlimited(),
                    },
                ],
            }
        );
        config.ServiceType.Returns(SupportedServices.Radarr);

        var guide = CreateQualityQuery(
            [
                new RadarrQualitySizeResource
                {
                    Type = "real",
                    Qualities = [NewQualitySize.Item("quality1", 0, 100, 90)],
                },
            ],
            []
        );

        var sut = new QualitySizeConfigPhase(log, guide, config, limitFactory);
        var context = new QualitySizePipelineContext();

        await sut.Execute(context, CancellationToken.None);

        // TestQualityItemLimitFactory uses TestQualityItemLimits (400 for both)
        context
            .Qualities.Select(x => x.Item)
            .Should()
            .BeEquivalentTo([NewQualitySize.Item("quality1", 0, 400, 400)]);
    }
}
