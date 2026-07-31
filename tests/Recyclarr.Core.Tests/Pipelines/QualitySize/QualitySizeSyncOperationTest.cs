using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Pipelines;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Pipelines.QualitySize.PipelinePhases.Limits;
using Recyclarr.Servarr.QualitySize;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;
using Recyclarr.Sync.Results;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Core.Tests.Pipelines.QualitySize;

internal sealed class QualitySizeSyncOperationTest
{
    [Test]
    public async Task Compute_returns_only_changed_semantic_values_and_retains_local_failure()
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
            api,
            limitFactory,
            NewConfig.Radarr()
        );
        var plan = new TestPlan
        {
            QualitySizes = new PlannedQualitySizes
            {
                Type = "movie",
                Qualities =
                [
                    new PlannedQualityItem("Bluray-1080p", 5, null, null),
                    new PlannedQualityItem("WEB-1080p", 3, 80, 40),
                    new PlannedQualityItem("HDTV-1080p", 2, null, null),
                    new PlannedQualityItem("Missing", 5, 100, 50),
                ],
            },
        };

        var result = await Execute(sut, plan, new RecordingPipelinePublisher());
        result.Status.Should().Be(SyncResultStatus.Partial);
        result
            .Outcomes.Should()
            .ContainSingle()
            .Which.Should()
            .Be(new QualitySizeServiceQualityNotFoundOutcome("Missing"));
        result.Deltas.Should().ContainSingle();
        var delta = result.Deltas[0];
        delta.Quality.Should().Be("Bluray-1080p");
        delta
            .Components.Should()
            .Equal(
                new QualitySizeMinimumChanged(
                    new ValueDelta<QualitySizeValue>(
                        new QualitySizeValue.Numeric(1),
                        new QualitySizeValue.Numeric(5)
                    )
                ),
                new QualitySizePreferredChanged(
                    new ValueDelta<QualitySizeValue>(
                        new QualitySizeValue.Numeric(80),
                        new QualitySizeValue.Unlimited()
                    )
                ),
                new QualitySizeMaximumChanged(
                    new ValueDelta<QualitySizeValue>(
                        new QualitySizeValue.Numeric(100),
                        new QualitySizeValue.Unlimited()
                    )
                )
            );
    }

    [Test]
    public async Task Compute_maps_plan_rejections_to_typed_outcomes()
    {
        var api = Substitute.For<IQualityDefinitionService>();
        api.GetQualityDefinitions(default).ReturnsForAnyArgs([]);
        var limitFactory = Substitute.For<IQualityItemLimitFactory>();
        limitFactory.Create(default, default).ReturnsForAnyArgs(new QualityItemLimits(400, 400));
        var sut = new QualitySizeSyncOperation(
            Substitute.For<ILogger>(),
            api,
            limitFactory,
            NewConfig.Radarr()
        );
        var plan = new TestPlan { QualitySizes = NewPlan.Qs("movie") };
        plan.Add(new QualityDefinitionNotFoundOutcome("movie"));
        plan.Add(new QualityNotFoundOutcome("Unknown", "movie"));
        plan.Add(new MinGreaterThanPreferredOutcome("Bluray-1080p", 75, 50));
        plan.Add(new UnlimitedPreferredGreaterThanMaxOutcome("WEB-1080p", 100));
        plan.Add(new PreferredGreaterThanMaxOutcome("HDTV-1080p", 75, 50));
        plan.Add(new PreferredRatioClampedOutcome(2, 1));

        var result = await Execute(sut, plan, new RecordingPipelinePublisher());
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
                new QualitySizePreferredRatioClampedOutcome(new ValueDelta<decimal>(2, 1))
            );
    }

    [Test]
    public async Task Missing_server_quality_retains_name_and_skips_item()
    {
        var api = Substitute.For<IQualityDefinitionService>();
        api.GetQualityDefinitions(default).ReturnsForAnyArgs([]);
        var limitFactory = Substitute.For<IQualityItemLimitFactory>();
        limitFactory.Create(default, default).ReturnsForAnyArgs(new QualityItemLimits(400, 400));
        var sut = new QualitySizeSyncOperation(
            Substitute.For<ILogger>(),
            api,
            limitFactory,
            NewConfig.Radarr()
        );
        var plan = new TestPlan
        {
            QualitySizes = new PlannedQualitySizes
            {
                Type = "movie",
                Qualities = [new PlannedQualityItem("Bluray-1080p", 5, 100, 50)],
            },
        };
        var publisher = new RecordingPipelinePublisher();

        var result = await Execute(sut, plan, publisher);

        result.Status.Should().Be(SyncResultStatus.Failed);
        publisher
            .Outcomes.Should()
            .ContainSingle()
            .Which.Should()
            .Be(new MissingServerQualityDefinitionOutcome("Bluray-1080p"));
    }

    private static async Task<QualitySizePipelineResult> Execute(
        QualitySizeSyncOperation sut,
        PipelinePlan plan,
        IPipelinePublisher publisher
    )
    {
        var result = await ((ISyncOperation)sut).Execute(
            preview: true,
            plan,
            publisher,
            _ => { },
            CancellationToken.None
        );
        return result.Should().BeOfType<QualitySizePipelineResult>().Which;
    }

    private sealed class RecordingPipelinePublisher : IPipelinePublisher
    {
        public List<SyncOutcome> Outcomes { get; } = [];

        public void Add(SyncOutcome outcome) => Outcomes.Add(outcome);

        public void AddError(string message) { }

        public void AddWarning(string message) { }

        public void AddDeprecation(string message) { }

        public void SetStatus(
            PipelineProgressStatus status,
            int? count = null,
            PipelineItemChanges? changes = null
        ) { }
    }
}
