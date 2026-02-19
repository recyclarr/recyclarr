using System.Diagnostics.CodeAnalysis;
using Autofac.Features.Metadata;
using Recyclarr.Cli.Migration;
using Recyclarr.Cli.Migration.Steps;
using Spectre.Console.Testing;

namespace Recyclarr.Cli.Tests.Migration;

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
        using var console = new TestConsole();
        var log = Substitute.For<ILogger>();
        var step = Substitute.For<IMigrationStep>();
        var executor = new MigrationExecutor(WrapWithMetadata([step]), console, log);

        step.CheckIfNeeded().Returns(false);

        executor.PerformAllMigrationSteps();

        step.Received().CheckIfNeeded();
        step.DidNotReceiveWithAnyArgs().Execute(default!);
    }

    [Test]
    public void Step_executed_if_check_returns_true()
    {
        using var console = new TestConsole();
        var log = Substitute.For<ILogger>();
        var step = Substitute.For<IMigrationStep>();
        var executor = new MigrationExecutor(WrapWithMetadata([step]), console, log);

        step.CheckIfNeeded().Returns(true);

        executor.PerformAllMigrationSteps();

        step.Received().CheckIfNeeded();
        step.ReceivedWithAnyArgs().Execute(default!);
    }

    [Test]
    public void Steps_executed_in_ascending_order()
    {
        using var console = new TestConsole();
        var log = Substitute.For<ILogger>();

        var steps = new[]
        {
            Substitute.For<IMigrationStep>(),
            Substitute.For<IMigrationStep>(),
            Substitute.For<IMigrationStep>(),
        };

        var executor = new MigrationExecutor(WrapWithMetadata(steps), console, log);

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
        using var console = new TestConsole();
        var log = Substitute.For<ILogger>();
        var step = Substitute.For<IMigrationStep>();
        var executor = new MigrationExecutor(WrapWithMetadata([step]), console, log);

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
        using var console = new TestConsole();
        var log = Substitute.For<ILogger>();
        var step = Substitute.For<IMigrationStep>();
        var executor = new MigrationExecutor(WrapWithMetadata([step]), console, log);
        var exception = new MigrationException(new ArgumentException(), "a", ["b"]);

        step.CheckIfNeeded().Returns(true);
        step.When(x => x.Execute(Arg.Any<ILogger>())).Throw(exception);

        var act = () => executor.PerformAllMigrationSteps();

        act.Should().Throw<MigrationException>().Which.Should().Be(exception);
    }
}
