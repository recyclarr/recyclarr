using Recyclarr.Cli.Migration;
using Recyclarr.Cli.Migration.Steps;
using Recyclarr.Cli.TestLibrary;
using Spectre.Console.Testing;

namespace Recyclarr.Cli.Tests.Migration;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class MigrationExecutorTest : CliIntegrationFixture
{
    [Test]
    public void Migration_steps_are_in_expected_order()
    {
        var steps = Resolve<IEnumerable<IMigrationStep>>();
        var orderedSteps = steps.OrderBy(x => x.Order).Select(x => x.GetType()).ToList();
        orderedSteps.Should().BeEquivalentTo(
            new[]
            {
                typeof(MigrateTrashYml),
                typeof(MigrateTrashUpdaterAppDataDir)
            },
            config => config.WithStrictOrdering());
    }

    [Test]
    public void Step_not_executed_if_check_returns_false()
    {
        using var console = new TestConsole();
        var step = Substitute.For<IMigrationStep>();
        var executor = new MigrationExecutor(new[] {step}, console);

        step.CheckIfNeeded().Returns(false);

        executor.PerformAllMigrationSteps(false);

        step.Received().CheckIfNeeded();
        step.DidNotReceive().Execute(null);
    }

    [Test]
    public void Step_executed_if_check_returns_true()
    {
        using var console = new TestConsole();
        var step = Substitute.For<IMigrationStep>();
        var executor = new MigrationExecutor(new[] {step}, console);

        step.CheckIfNeeded().Returns(true);

        executor.PerformAllMigrationSteps(false);

        step.Received().CheckIfNeeded();
        step.Received().Execute(null);
    }

    [Test]
    public void Steps_executed_in_ascending_order()
    {
        using var console = new TestConsole();

        var steps = new[]
        {
            Substitute.For<IMigrationStep>(),
            Substitute.For<IMigrationStep>(),
            Substitute.For<IMigrationStep>()
        };

        steps[0].Order.Returns(20);
        steps[1].Order.Returns(10);
        steps[2].Order.Returns(30);

        var executor = new MigrationExecutor(steps, console);

        executor.PerformAllMigrationSteps(false);

        Received.InOrder(() =>
        {
            steps[1].CheckIfNeeded();
            steps[0].CheckIfNeeded();
            steps[2].CheckIfNeeded();
        });
    }

    [Test]
    public void Exception_converted_to_migration_exception()
    {
        using var console = new TestConsole();
        var step = Substitute.For<IMigrationStep>();
        var executor = new MigrationExecutor(new[] {step}, console);

        step.CheckIfNeeded().Returns(true);
        step.When(x => x.Execute(null)).Throw(new ArgumentException("test message"));

        var act = () => executor.PerformAllMigrationSteps(false);

        act.Should().Throw<MigrationException>().Which.OriginalException.Message.Should().Be("test message");
    }

    [Test]
    public void Migration_exceptions_are_not_converted()
    {
        using var console = new TestConsole();
        var step = Substitute.For<IMigrationStep>();
        var executor = new MigrationExecutor(new[] {step}, console);
        var exception = new MigrationException(new ArgumentException(), "a", new[] {"b"});

        step.CheckIfNeeded().Returns(true);
        step.When(x => x.Execute(null)).Throw(exception);

        var act = () => executor.PerformAllMigrationSteps(false);

        act.Should().Throw<MigrationException>().Which.Should().Be(exception);
    }
}
