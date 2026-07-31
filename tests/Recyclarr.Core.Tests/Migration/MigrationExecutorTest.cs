using System.Diagnostics.CodeAnalysis;
using Autofac.Features.Metadata;
using Recyclarr.Migration;
using Recyclarr.Migration.Steps;

namespace Recyclarr.Core.Tests.Migration;

internal sealed class MigrationExecutorTest
{
    private static List<Meta<IMigrationStep>> WrapWithMetadata(IEnumerable<IMigrationStep> steps)
    {
        return steps
            .Select(
                (step, index) =>
                    new Meta<IMigrationStep>(
                        step,
                        new Dictionary<string, object?> { ["Order"] = index }
                    )
            )
            .ToList();
    }

    [Test]
    public void Step_not_executed_if_check_returns_false()
    {
        var log = Substitute.For<ILogger>();
        var step = Substitute.For<IMigrationStep>();
        var executor = new MigrationExecutor(WrapWithMetadata([step]), log);

        step.CheckIfNeeded().Returns(false);

        executor.PerformAllMigrationSteps();

        step.Received().CheckIfNeeded();
        step.DidNotReceiveWithAnyArgs().Execute(default!);
    }

    [Test]
    public void Step_executed_if_check_returns_true()
    {
        var log = Substitute.For<ILogger>();
        var step = Substitute.For<IMigrationStep>();
        var executor = new MigrationExecutor(WrapWithMetadata([step]), log);

        step.CheckIfNeeded().Returns(true);

        executor.PerformAllMigrationSteps();

        step.Received().CheckIfNeeded();
        step.ReceivedWithAnyArgs().Execute(default!);
    }

    [Test]
    public void Steps_executed_in_ascending_order()
    {
        var log = Substitute.For<ILogger>();

        var steps = new[]
        {
            Substitute.For<IMigrationStep>(),
            Substitute.For<IMigrationStep>(),
            Substitute.For<IMigrationStep>(),
        };

        var executor = new MigrationExecutor(WrapWithMetadata(steps), log);

        executor.PerformAllMigrationSteps();

        Received.InOrder(() =>
        {
            steps[0].CheckIfNeeded();
            steps[1].CheckIfNeeded();
            steps[2].CheckIfNeeded();
        });
    }

    [Test]
    public void Exception_converted_to_migration_exception()
    {
        var log = Substitute.For<ILogger>();
        var step = Substitute.For<IMigrationStep>();
        var executor = new MigrationExecutor(WrapWithMetadata([step]), log);

        step.CheckIfNeeded().Returns(true);
        step.When(x => x.Execute(Arg.Any<ILogger>())).Throw(new ArgumentException("test message"));

        var act = () => executor.PerformAllMigrationSteps();

        act.Should()
            .Throw<MigrationException>()
            .Which.OriginalException.Message.Should()
            .Be("test message");
    }

    [Test]
    [SuppressMessage(
        "SonarLint",
        "S3928:Parameter names used into ArgumentException constructors should match an existing one",
        Justification = "Used in unit test only"
    )]
    public void Migration_exceptions_are_not_converted()
    {
        var log = Substitute.For<ILogger>();
        var step = Substitute.For<IMigrationStep>();
        var executor = new MigrationExecutor(WrapWithMetadata([step]), log);
        var exception = new MigrationException(new ArgumentException(), "a", ["b"]);

        step.CheckIfNeeded().Returns(true);
        step.When(x => x.Execute(Arg.Any<ILogger>())).Throw(exception);

        var act = () => executor.PerformAllMigrationSteps();

        act.Should().Throw<MigrationException>().Which.Should().Be(exception);
    }
}
