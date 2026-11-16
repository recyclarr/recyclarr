using Recyclarr.Migration;
using Recyclarr.Migration.Steps;

namespace Recyclarr.Core.Tests.Migration;

internal sealed class MigrationExecutorTest
{
    // OrderBy is stable, so a constant key keeps the given order
    private static MigrationExecutor CreateSut(params IMigrationStep[] steps) =>
        new(steps.OrderBy(_ => 0), Substitute.For<ILogger>());

    [Test]
    public void Steps_run_in_order()
    {
        IMigrationStep[] steps =
        [
            Substitute.For<IMigrationStep>(),
            Substitute.For<IMigrationStep>(),
            Substitute.For<IMigrationStep>(),
        ];

        CreateSut(steps).RunAll();

        Received.InOrder(() =>
        {
            steps[0].Run(Arg.Any<ILogger>());
            steps[1].Run(Arg.Any<ILogger>());
            steps[2].Run(Arg.Any<ILogger>());
        });
    }

    [Test]
    public void Failure_names_step_cause_and_fixes_and_stops_later_steps()
    {
        var failing = Substitute.For<IMigrationStep>();
        failing.Description.Returns("Move things");
        failing.Remediation.Returns(["Fix one", "Fix two"]);
        failing.When(x => x.Run(Arg.Any<ILogger>())).Throw(new IOException("disk full"));
        var later = Substitute.For<IMigrationStep>();

        var act = () => CreateSut(failing, later).RunAll();

        act.Should()
            .Throw<MigrationException>()
            .WithMessage("*Move things*disk full*Fix one*Fix two*")
            .WithInnerException<IOException>();
        later.DidNotReceiveWithAnyArgs().Run(default!);
    }
}
