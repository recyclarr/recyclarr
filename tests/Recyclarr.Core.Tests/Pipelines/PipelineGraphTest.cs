using Recyclarr.Pipelines;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;

namespace Recyclarr.Core.Tests.Pipelines;

internal sealed class PipelineGraphTest
{
    [Test]
    public void Dependencies_order_pipelines_and_ties_keep_registration_order()
    {
        var sut = new PipelineGraph([
            Pipeline<PlanA>(PipelineType.QualityProfile, PipelineType.CustomFormat),
            Pipeline<PlanB>(PipelineType.MediaNaming),
            Pipeline<PlanC>(PipelineType.CustomFormat),
        ]);

        sut.Pipelines.Select(x => x.Type)
            .Should()
            .Equal(
                PipelineType.MediaNaming,
                PipelineType.CustomFormat,
                PipelineType.QualityProfile
            );
    }

    [Test]
    public void Dependency_on_unregistered_pipeline_is_rejected()
    {
        var act = () =>
            new PipelineGraph([
                Pipeline<PlanA>(PipelineType.QualityProfile, PipelineType.CustomFormat),
            ]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*unregistered*CustomFormat*");
    }

    [Test]
    public void Cycle_is_rejected()
    {
        var act = () =>
            new PipelineGraph([
                Pipeline<PlanA>(PipelineType.CustomFormat, PipelineType.QualityProfile),
                Pipeline<PlanB>(PipelineType.QualityProfile, PipelineType.CustomFormat),
            ]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Cycle*");
    }

    [Test]
    public void Duplicate_pipeline_is_rejected()
    {
        var act = () =>
            new PipelineGraph([
                Pipeline<PlanA>(PipelineType.CustomFormat),
                Pipeline<PlanB>(PipelineType.CustomFormat),
            ]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*once*CustomFormat*");
    }

    [Test]
    public void Planner_reads_a_declared_dependency_plan()
    {
        var upstream = new PlanA();
        PlanA? seen = null;
        // Registered before its dependency, so only the graph order makes the plan available
        var graph = new PipelineGraph([
            Pipeline(
                PipelineType.QualityProfile,
                context =>
                {
                    seen = context.Require<PlanA>();
                    return new PlanB();
                },
                PipelineType.CustomFormat
            ),
            Pipeline(PipelineType.CustomFormat, _ => upstream),
        ]);

        new PlanBuilder(graph, Substitute.For<ILogger>()).Build();

        seen.Should().BeSameAs(upstream);
    }

    [Test]
    public void Planner_reading_an_undeclared_pipeline_plan_fails()
    {
        var graph = new PipelineGraph([
            Pipeline(PipelineType.CustomFormat, _ => new PlanA()),
            Pipeline(
                PipelineType.QualityProfile,
                context =>
                {
                    context.Require<PlanA>();
                    return new PlanB();
                }
            ),
        ]);

        var act = () => new PlanBuilder(graph, Substitute.For<ILogger>()).Build();

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*QualityProfile*PlanA*no declared dependency*");
    }

    private static Pipeline<TPlan> Pipeline<TPlan>(
        PipelineType type,
        params PipelineType[] dependencies
    )
        where TPlan : class, new() => Pipeline(type, _ => new TPlan(), dependencies);

    private static Pipeline<TPlan> Pipeline<TPlan>(
        PipelineType type,
        Func<PlanningContext, TPlan> plan,
        params PipelineType[] dependencies
    )
        where TPlan : class
    {
        var planner = Substitute.For<IPipelinePlanner<TPlan>>();
        planner.Plan(default!).ReturnsForAnyArgs(x => plan(x.Arg<PlanningContext>()));
        return new Pipeline<TPlan>(
            type,
            dependencies,
            planner,
            Substitute.For<ISyncOperation<TPlan>>()
        );
    }

    // Proxied by NSubstitute, so they cannot be private
    internal sealed class PlanA;

    internal sealed class PlanB;

    internal sealed class PlanC;
}
