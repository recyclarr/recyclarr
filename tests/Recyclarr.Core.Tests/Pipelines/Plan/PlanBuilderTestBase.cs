using Autofac;
using Recyclarr.Config.Models;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;

namespace Recyclarr.Core.Tests.Pipelines.Plan;

internal abstract class PlanBuilderTestBase : CoreIntegrationTestFixture
{
    protected (PlanBuilder Sut, IInstancePublisher Publisher) CreatePlanBuilder(
        IServiceConfiguration config
    )
    {
        var publisher = Substitute.For<IInstancePublisher>();
        var scope = ResolveWithConfig<PlanBuilder>(
            config,
            c => c.RegisterInstance(publisher).As<IInstancePublisher>()
        );
        return (scope.Entry, publisher);
    }
}
