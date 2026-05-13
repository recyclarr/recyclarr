using FluentValidation.Results;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Pipelines.QualityProfile.Models;
using Recyclarr.Servarr.QualityProfile;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Cli.Tests.Pipelines.QualityProfile;

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
        var publisher = Substitute.For<IPipelinePublisher>();

        _sut.LogPersistenceResults(transactions, publisher);

        publisher.Received().SetStatus(PipelineProgressStatus.Succeeded, 0);
    }

    [Test]
    public void Status_is_partial_when_errors_and_valid_profiles_exist()
    {
        var transactions = new QualityProfileTransactionData();
        transactions.UnchangedProfiles.Add(CreateProfile("good"));
        transactions.InvalidProfiles.Add(
            new InvalidProfileData(CreateProfile("bad"), [new ValidationFailure("x", "error")])
        );
        var publisher = Substitute.For<IPipelinePublisher>();

        _sut.LogPersistenceResults(transactions, publisher);

        publisher.Received().SetStatus(PipelineProgressStatus.Partial, 0);
    }

    [Test]
    public void Status_is_failed_when_all_profiles_have_errors()
    {
        var transactions = new QualityProfileTransactionData();
        transactions.InvalidProfiles.Add(
            new InvalidProfileData(CreateProfile("bad"), [new ValidationFailure("x", "error")])
        );
        var publisher = Substitute.For<IPipelinePublisher>();

        _sut.LogPersistenceResults(transactions, publisher);

        publisher.Received().SetStatus(PipelineProgressStatus.Failed, 0);
    }

    [Test]
    public void Status_is_partial_with_rename_conflicts_and_valid_profiles()
    {
        var transactions = new QualityProfileTransactionData();
        transactions.NewProfiles.Add(CreateProfile("good"));
        transactions.RenameConflicts.Add("conflict");
        var publisher = Substitute.For<IPipelinePublisher>();

        _sut.LogPersistenceResults(transactions, publisher);

        publisher.Received().SetStatus(PipelineProgressStatus.Partial, 1);
    }

    [Test]
    public void Status_is_failed_with_only_ambiguous_profiles()
    {
        var transactions = new QualityProfileTransactionData();
        transactions.AmbiguousProfiles.Add(
            new AmbiguousQualityProfile(NewPlan.Qp("ambiguous"), [("dup1", 1), ("dup2", 2)])
        );
        var publisher = Substitute.For<IPipelinePublisher>();

        _sut.LogPersistenceResults(transactions, publisher);

        publisher.Received().SetStatus(PipelineProgressStatus.Failed, 0);
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
        var publisher = Substitute.For<IPipelinePublisher>();

        _sut.LogPersistenceResults(transactions, publisher);

        publisher.Received().SetStatus(PipelineProgressStatus.Partial, 1);
    }
}
