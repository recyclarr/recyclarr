using FluentValidation.Results;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines.QualityProfile;
using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.Cli.Tests.Reusable;
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

    private static QualityProfilePipelineContext CreateContext()
    {
        return new QualityProfilePipelineContext
        {
            InstanceName = "test",
            SyncSettings = Substitute.For<ISyncSettings>(),
            Publisher = Substitute.For<IPipelinePublisher>(),
            TransactionOutput = new QualityProfileTransactionData(),
        };
    }

    [Test]
    public void Status_is_succeeded_when_no_errors()
    {
        var context = CreateContext();
        context.TransactionOutput.UnchangedProfiles.Add(CreateProfile("good"));

        _sut.LogPersistenceResults(context);

        context.Publisher.Received().SetStatus(PipelineProgressStatus.Succeeded, 0);
    }

    [Test]
    public void Status_is_partial_when_errors_and_valid_profiles_exist()
    {
        var context = CreateContext();
        context.TransactionOutput.UnchangedProfiles.Add(CreateProfile("good"));
        context.TransactionOutput.InvalidProfiles.Add(
            new InvalidProfileData(CreateProfile("bad"), [new ValidationFailure("x", "error")])
        );

        _sut.LogPersistenceResults(context);

        context.Publisher.Received().SetStatus(PipelineProgressStatus.Partial, 0);
    }

    [Test]
    public void Status_is_failed_when_all_profiles_have_errors()
    {
        var context = CreateContext();
        context.TransactionOutput.InvalidProfiles.Add(
            new InvalidProfileData(CreateProfile("bad"), [new ValidationFailure("x", "error")])
        );

        _sut.LogPersistenceResults(context);

        context.Publisher.Received().SetStatus(PipelineProgressStatus.Failed, 0);
    }

    [Test]
    public void Status_is_partial_with_conflicting_profiles_and_valid_profiles()
    {
        var context = CreateContext();
        context.TransactionOutput.NewProfiles.Add(CreateProfile("good"));
        context.TransactionOutput.ConflictingProfiles.Add(
            new ConflictingQualityProfile(NewPlan.Qp("conflict"), 99)
        );

        _sut.LogPersistenceResults(context);

        context.Publisher.Received().SetStatus(PipelineProgressStatus.Partial, 1);
    }

    [Test]
    public void Status_is_failed_with_only_ambiguous_profiles()
    {
        var context = CreateContext();
        context.TransactionOutput.AmbiguousProfiles.Add(
            new AmbiguousQualityProfile(NewPlan.Qp("ambiguous"), [("dup1", 1), ("dup2", 2)])
        );

        _sut.LogPersistenceResults(context);

        context.Publisher.Received().SetStatus(PipelineProgressStatus.Failed, 0);
    }

    [Test]
    public void Status_is_partial_with_updated_profiles_and_errors()
    {
        var context = CreateContext();
        context.TransactionOutput.UpdatedProfiles.Add(
            new ProfileWithStats { Profile = CreateProfile("updated") }
        );
        context.TransactionOutput.InvalidProfiles.Add(
            new InvalidProfileData(CreateProfile("bad"), [new ValidationFailure("x", "error")])
        );

        _sut.LogPersistenceResults(context);

        context.Publisher.Received().SetStatus(PipelineProgressStatus.Partial, 1);
    }
}
