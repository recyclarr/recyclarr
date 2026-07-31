using Recyclarr.Pipelines;
using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Pipelines.CustomFormat.Models;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;
using Recyclarr.SyncState;

namespace Recyclarr.Core.Tests.Pipelines.CustomFormat;

internal sealed class CustomFormatTransactionLoggerTest
{
    private readonly CustomFormatTransactionLogger _sut = new(Substitute.For<ILogger>());

    [Test]
    public void Ambiguous_matches_retain_outcome_and_interrupt_pipeline()
    {
        var transactions = new CustomFormatTransactionData();
        transactions.AmbiguousCustomFormats.Add(
            new AmbiguousMatch("HDR", [("HDR", 1), ("HDR", 2)])
        );
        var publisher = new RecordingPipelinePublisher();

        var act = () => _sut.LogTransactions(transactions, publisher);

        act.Should().Throw<PipelineInterruptException>();
        publisher
            .Outcomes.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(new AmbiguousCustomFormatOutcome("HDR", [("HDR", 1), ("HDR", 2)]));
        publisher.Status.Should().BeNull();
    }

    [Test]
    public void Replacements_retain_outcome_and_pipeline_succeeds()
    {
        var transactions = new CustomFormatTransactionData();
        transactions.ReplacedCustomFormats.Add("HDR");
        var publisher = new RecordingPipelinePublisher();

        _sut.LogTransactions(transactions, publisher);

        publisher
            .Outcomes.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(new ReplacedCustomFormatsOutcome(["HDR"]));
        publisher.Status.Should().Be(PipelineProgressStatus.Succeeded);
        publisher.Count.Should().Be(0);
    }

    private sealed class RecordingPipelinePublisher : IPipelinePublisher
    {
        public List<SyncOutcome> Outcomes { get; } = [];
        public PipelineProgressStatus? Status { get; private set; }
        public int? Count { get; private set; }

        public void Add(SyncOutcome outcome) => Outcomes.Add(outcome);

        public void AddError(string message) { }

        public void AddWarning(string message) { }

        public void AddDeprecation(string message) { }

        public void SetStatus(
            PipelineProgressStatus status,
            int? count = null,
            PipelineItemChanges? changes = null
        )
        {
            Status = status;
            Count = count;
        }
    }
}
