using Recyclarr.Core.TestLibrary;
using Recyclarr.Pipelines;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Pipelines.QualitySize.PipelinePhases.Limits;
using Recyclarr.Servarr.QualitySize;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Core.Tests.Pipelines.QualitySize;

internal sealed class QualitySizeSyncOperationTest
{
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

        var result = await ((ISyncOperation)sut).Compute(plan, publisher, CancellationToken.None);

        result.Should().BeOfType<QualitySizeComputeResult>().Which.Items.Should().BeEmpty();
        publisher
            .Outcomes.Should()
            .ContainSingle()
            .Which.Should()
            .Be(new MissingServerQualityDefinitionOutcome("Bluray-1080p"));
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
