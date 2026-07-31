using FluentValidation.Results;
using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Pipelines.QualityProfile.Models;
using Recyclarr.Servarr.QualityProfile;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Core.Tests.Pipelines.QualityProfile;

internal sealed class QualityProfileLoggerTest
{
    private readonly QualityProfileLogger _sut = new(Substitute.For<ILogger>());

    private static UpdatedQualityProfile CreateProfile(string name)
    {
        return new UpdatedQualityProfile
        {
            Profile = new QualityProfileData { Name = name },
            ProfileConfig = NewPlan.Qp(name),
        };
    }

    [Test]
    public void Status_is_succeeded_when_no_errors()
    {
        var transactions = new QualityProfileTransactionData();
        transactions.UnchangedProfiles.Add(CreateProfile("good"));
        var publisher = new RecordingPipelinePublisher();

        _sut.LogPersistenceResults(transactions, publisher);

        publisher.Status.Should().Be(PipelineProgressStatus.Succeeded);
        publisher.Count.Should().Be(0);
    }

    [Test]
    public void Status_is_partial_when_errors_and_valid_profiles_exist()
    {
        var transactions = new QualityProfileTransactionData();
        transactions.UnchangedProfiles.Add(CreateProfile("good"));
        transactions.InvalidProfiles.Add(
            new InvalidProfileData(CreateProfile("bad"), [new ValidationFailure("x", "error")])
        );
        var publisher = new RecordingPipelinePublisher();

        _sut.LogPersistenceResults(transactions, publisher);

        publisher.Status.Should().Be(PipelineProgressStatus.Partial);
        publisher.Count.Should().Be(0);
    }

    [Test]
    public void Status_is_failed_when_all_profiles_have_errors()
    {
        var transactions = new QualityProfileTransactionData();
        transactions.InvalidProfiles.Add(
            new InvalidProfileData(CreateProfile("bad"), [new ValidationFailure("x", "error")])
        );
        var publisher = new RecordingPipelinePublisher();

        _sut.LogPersistenceResults(transactions, publisher);

        publisher.Status.Should().Be(PipelineProgressStatus.Failed);
        publisher.Count.Should().Be(0);
    }

    [Test]
    public void Status_is_partial_with_rename_conflicts_and_valid_profiles()
    {
        var transactions = new QualityProfileTransactionData();
        transactions.NewProfiles.Add(CreateProfile("good"));
        transactions.RenameConflicts.Add("conflict");
        var publisher = new RecordingPipelinePublisher();

        _sut.LogPersistenceResults(transactions, publisher);

        publisher.Status.Should().Be(PipelineProgressStatus.Partial);
        publisher.Count.Should().Be(1);
    }

    [Test]
    public void Status_is_failed_with_only_ambiguous_profiles()
    {
        var transactions = new QualityProfileTransactionData();
        transactions.AmbiguousProfiles.Add(
            new AmbiguousQualityProfile(NewPlan.Qp("ambiguous"), [("dup1", 1), ("dup2", 2)])
        );
        var publisher = new RecordingPipelinePublisher();

        _sut.LogPersistenceResults(transactions, publisher);

        publisher.Status.Should().Be(PipelineProgressStatus.Failed);
        publisher.Count.Should().Be(0);
    }

    [Test]
    public void Status_is_partial_with_updated_profiles_and_errors()
    {
        var transactions = new QualityProfileTransactionData();
        transactions.UpdatedProfiles.Add(
            new ProfileWithStats { Profile = CreateProfile("updated") }
        );
        transactions.InvalidProfiles.Add(
            new InvalidProfileData(CreateProfile("bad"), [new ValidationFailure("x", "error")])
        );
        var publisher = new RecordingPipelinePublisher();

        _sut.LogPersistenceResults(transactions, publisher);

        publisher.Status.Should().Be(PipelineProgressStatus.Partial);
        publisher.Count.Should().Be(1);
    }

    [Test]
    public void Transaction_notices_retain_structured_conditions()
    {
        var profile = CreateProfile("WEB") with
        {
            UpdatedQualities = new UpdatedQualities { InvalidQualityNames = ["Unknown Quality"] },
            InvalidExceptCfNames = ["Unknown CF"],
            InvalidExceptCfPatterns = ["missing.*"],
        };
        var transactions = new QualityProfileTransactionData();
        transactions.NonExistentProfiles.Add("Missing Profile");
        transactions.InvalidProfiles.Add(
            new InvalidProfileData(
                CreateProfile("Invalid Profile"),
                [new ValidationFailure("Cutoff", "Invalid cutoff")]
            )
        );
        transactions.ReplacedProfiles.Add("Replaced Profile");
        transactions.RenameConflicts.Add("Existing Profile");
        transactions.AmbiguousProfiles.Add(
            new AmbiguousQualityProfile(NewPlan.Qp("Ambiguous Profile"), [("one", 1), ("two", 2)])
        );
        transactions.NewProfiles.Add(profile);
        var publisher = new RecordingPipelinePublisher();

        _sut.LogTransactionNotices(transactions, publisher);

        SyncOutcome[] expected =
        [
            new NonExistentQualityProfilesOutcome(["Missing Profile"]),
            new InvalidQualityProfileOutcome(
                "Invalid Profile",
                "Cutoff",
                "Invalid cutoff",
                null,
                null
            ),
            new ReplacedQualityProfilesOutcome(["Replaced Profile"]),
            new QualityProfileRenameConflictOutcome("Existing Profile"),
            new AmbiguousQualityProfileOutcome("Ambiguous Profile", [("one", 1), ("two", 2)]),
            new InvalidQualityNamesOutcome("WEB", ["Unknown Quality"]),
            new InvalidExceptCustomFormatNamesOutcome("WEB", ["Unknown CF"]),
            new UnmatchedExceptCustomFormatPatternsOutcome("WEB", ["missing.*"]),
        ];
        publisher
            .Outcomes.Should()
            .BeEquivalentTo(expected, options => options.WithStrictOrdering());
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
