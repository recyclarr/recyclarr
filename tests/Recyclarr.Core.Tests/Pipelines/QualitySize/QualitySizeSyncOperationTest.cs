using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Pipelines;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Pipelines.QualitySize.PipelinePhases.Limits;
using Recyclarr.Servarr.QualitySize;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Core.Tests.Pipelines.QualitySize;

internal sealed class QualitySizeSyncOperationTest
{
    [Test]
    public async Task Compute_returns_every_planned_quality_and_reports_missing_quality()
    {
        var api = Substitute.For<IQualityDefinitionService>();
        api.GetQualityDefinitions(default)
            .ReturnsForAnyArgs([
                new QualityDefinitionItem
                {
                    QualityName = "Bluray-1080p",
                    MinSize = 1,
                    MaxSize = 100,
                    PreferredSize = 80,
                },
                new QualityDefinitionItem
                {
                    QualityName = "WEB-1080p",
                    MinSize = 3,
                    MaxSize = 80,
                    PreferredSize = 40,
                },
                new QualityDefinitionItem
                {
                    QualityName = "HDTV-1080p",
                    MinSize = 2,
                    MaxSize = 400,
                    PreferredSize = 400,
                },
            ]);
        var limitFactory = Substitute.For<IQualityItemLimitFactory>();
        limitFactory.Create(default, default).ReturnsForAnyArgs(new QualityItemLimits(400, 400));
        var sut = new QualitySizeSyncOperation(
            Substitute.For<ILogger>(),
            new Lazy<IQualityDefinitionService>(api),
            limitFactory,
            NewConfig.Radarr()
        );
        var plan = new PlannedQualitySizes
        {
            Type = "movie",
            Qualities =
            [
                new PlannedQualityItem("Bluray-1080p", 5, null, null),
                new PlannedQualityItem("WEB-1080p", 3, 80, 40),
                new PlannedQualityItem("HDTV-1080p", 2, null, null),
                new PlannedQualityItem("Missing", 5, 100, 50),
            ],
        };

        var result = await Execute(sut, plan);
        result.Status.Should().Be(SyncResultStatus.Succeeded);
        result
            .Outcomes.Should()
            .ContainSingle()
            .Which.Should()
            .Be(new QualitySizeServiceQualityNotFoundOutcome("Missing"));
        QualitySizeValue unlimited = new QualitySizeValue.Unlimited();
        result
            .Resources.Should()
            .Equal(
                PlannedQualitySizeResource.Compared(
                    "Bluray-1080p",
                    (Numeric(1), Numeric(5)),
                    (Numeric(80), unlimited),
                    (Numeric(100), unlimited)
                ),
                PlannedQualitySizeResource.Compared(
                    "WEB-1080p",
                    (Numeric(3), Numeric(3)),
                    (Numeric(40), Numeric(40)),
                    (Numeric(80), Numeric(80))
                ),
                // A service size at the limit already means unlimited
                PlannedQualitySizeResource.Compared(
                    "HDTV-1080p",
                    (Numeric(2), Numeric(2)),
                    (unlimited, unlimited),
                    (unlimited, unlimited)
                )
            );
        result
            .Resources.Select(x => x.Action)
            .Should()
            .Equal(PlannedAction.Update, PlannedAction.Unchanged, PlannedAction.Unchanged);
    }

    private static QualitySizeValue.Numeric Numeric(decimal value) => new(value);

    [Test]
    public async Task Compute_maps_plan_rejections_to_typed_outcomes()
    {
        var api = Substitute.For<IQualityDefinitionService>();
        api.GetQualityDefinitions(default).ReturnsForAnyArgs([]);
        var limitFactory = Substitute.For<IQualityItemLimitFactory>();
        limitFactory.Create(default, default).ReturnsForAnyArgs(new QualityItemLimits(400, 400));
        var sut = new QualitySizeSyncOperation(
            Substitute.For<ILogger>(),
            new Lazy<IQualityDefinitionService>(api),
            limitFactory,
            NewConfig.Radarr()
        );
        SyncOutcome[] planOutcomes =
        [
            new QualityDefinitionNotFoundOutcome("movie"),
            new QualityNotFoundOutcome("Unknown", "movie"),
            new MinGreaterThanPreferredOutcome("Bluray-1080p", 75, 50),
            new UnlimitedPreferredGreaterThanMaxOutcome("WEB-1080p", 100),
            new PreferredGreaterThanMaxOutcome("HDTV-1080p", 75, 50),
            new PreferredRatioClampedOutcome(2, 1),
        ];

        var result = await Execute(sut, NewPlan.Qs("movie"), planOutcomes);
        result.Status.Should().Be(SyncResultStatus.Failed);
        result
            .Outcomes.Should()
            .Equal(
                new QualitySizeDefinitionReferenceMismatchOutcome("movie"),
                new QualitySizeReferenceMismatchOutcome("Unknown", "movie"),
                new QualitySizeMinimumGreaterThanPreferredOutcome(
                    "Bluray-1080p",
                    new QualitySizeValue.Numeric(75),
                    new QualitySizeValue.Numeric(50)
                ),
                new QualitySizeUnlimitedPreferredGreaterThanMaximumOutcome(
                    "WEB-1080p",
                    new QualitySizeValue.Unlimited(),
                    new QualitySizeValue.Numeric(100)
                ),
                new QualitySizePreferredGreaterThanMaximumOutcome(
                    "HDTV-1080p",
                    new QualitySizeValue.Numeric(75),
                    new QualitySizeValue.Numeric(50)
                ),
                new QualitySizePreferredRatioClampedOutcome(2, 1)
            );
    }

    [Test]
    // A quality the service lacks has no service data to be wrong, so it is not incomplete
    public async Task Missing_server_quality_is_reported_without_failing_the_pipeline()
    {
        var api = Substitute.For<IQualityDefinitionService>();
        api.GetQualityDefinitions(default).ReturnsForAnyArgs([]);
        var limitFactory = Substitute.For<IQualityItemLimitFactory>();
        limitFactory.Create(default, default).ReturnsForAnyArgs(new QualityItemLimits(400, 400));
        var sut = new QualitySizeSyncOperation(
            Substitute.For<ILogger>(),
            new Lazy<IQualityDefinitionService>(api),
            limitFactory,
            NewConfig.Radarr()
        );
        var plan = new PlannedQualitySizes
        {
            Type = "movie",
            Qualities = [new PlannedQualityItem("Bluray-1080p", 5, 100, 50)],
        };
        var result = await Execute(sut, plan);

        result.Status.Should().Be(SyncResultStatus.Succeeded);
        result
            .Outcomes.Should()
            .ContainSingle()
            .Which.Should()
            .Be(new QualitySizeServiceQualityNotFoundOutcome("Bluray-1080p"));
    }

    private static async Task<QualitySizePipelineResult> Execute(
        QualitySizeSyncOperation sut,
        PlannedQualitySizes plan,
        IReadOnlyList<SyncOutcome>? planOutcomes = null
    )
    {
        var result = await ((ISyncOperation<PlannedQualitySizes>)sut).Execute(
            preview: true,
            plan,
            planOutcomes ?? [],
            _ => { },
            CancellationToken.None
        );
        return result.Should().BeOfType<QualitySizePipelineResult>().Which;
    }
}
