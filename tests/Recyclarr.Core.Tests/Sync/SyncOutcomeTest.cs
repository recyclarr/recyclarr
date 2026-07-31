using Recyclarr.Config.Models;
using Recyclarr.ErrorHandling;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Sync;

namespace Recyclarr.Core.Tests.Sync;

internal sealed class SyncOutcomeTest
{
    [Test]
    public void Plan_retains_structured_outcomes_and_derives_error_state()
    {
        var publisher = new RecordingDiagnosticPublisher();
        var plan = new PipelinePlan(publisher);
        var warning = new PreferredRatioClampedOutcome(Original: 2, Clamped: 1);
        var error = new InvalidNamingFormatOutcome("Movie Folder Format", "missing");

        plan.Add(warning);
        plan.Add(error);

        plan.Outcomes.Should().Equal(warning, error);
        plan.HasErrors.Should().BeTrue();
        publisher.Outcomes.Should().Equal(warning, error);
    }

    [Test]
    public void Pipeline_publisher_retains_outcome_on_compatibility_event()
    {
        var runPublisher = new RecordingRunPublisher();
        var sut = new PipelinePublisher("instance", PipelineType.QualitySize, runPublisher);
        var outcome = new MissingServerQualityDefinitionOutcome("Bluray-1080p");

        sut.Add(outcome);

        runPublisher
            .Diagnostics.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new SyncDiagnosticEvent(
                    "instance",
                    SyncDiagnosticLevel.Warning,
                    "Server lacks quality definition for Bluray-1080p; it will be skipped",
                    outcome
                )
            );
    }

    [Test]
    public void Multi_message_outcome_is_retained_once()
    {
        var runPublisher = new RecordingRunPublisher();
        var sut = new PipelinePublisher("instance", PipelineType.CustomFormat, runPublisher);
        var outcome = new MigrationFailure("move state", "disk full", ["free space"]);

        sut.Add(outcome);

        runPublisher.Diagnostics.Should().HaveCount(3);
        runPublisher.Diagnostics[0].Outcome.Should().BeSameAs(outcome);
        runPublisher.Diagnostics.Skip(1).Should().OnlyContain(evt => evt.Outcome == null);
    }

    [Test]
    public void Instance_publisher_retains_multi_message_outcome_once()
    {
        var config = Substitute.For<IServiceConfiguration>();
        config.InstanceName.Returns("instance");
        var runPublisher = new RecordingRunPublisher();
        var sut = new InstancePublisher(config, runPublisher);
        var outcome = new MigrationFailure("move state", "disk full", ["free space"]);

        sut.Add(outcome);

        runPublisher
            .Diagnostics.Select(evt => (evt.Instance, evt.Level, evt.Message))
            .Should()
            .Equal(
                ("instance", SyncDiagnosticLevel.Error, "Migration step failed: move state"),
                ("instance", SyncDiagnosticLevel.Error, "Reason: disk full"),
                ("instance", SyncDiagnosticLevel.Error, "  - free space")
            );
        runPublisher.Diagnostics[0].Outcome.Should().BeSameAs(outcome);
        runPublisher.Diagnostics.Skip(1).Should().OnlyContain(evt => evt.Outcome == null);
    }

    private sealed class RecordingDiagnosticPublisher : IDiagnosticPublisher
    {
        public List<SyncOutcome> Outcomes { get; } = [];

        public void Add(SyncOutcome outcome) => Outcomes.Add(outcome);

        public void AddError(string message) { }

        public void AddWarning(string message) { }

        public void AddDeprecation(string message) { }
    }

    private sealed class RecordingRunPublisher : ISyncRunPublisher
    {
        public List<SyncDiagnosticEvent> Diagnostics { get; } = [];

        public void Publish(PipelineEvent evt) { }

        public void Publish(SyncDiagnosticEvent evt) => Diagnostics.Add(evt);
    }
}
