using Recyclarr.Config.Models;
using Recyclarr.Pipelines.Plan;

namespace Recyclarr.Core.Tests.Pipelines.Plan;

internal abstract class PlanBuilderTestBase : CoreIntegrationTestFixture
{
    protected PlanBuilder CreatePlanBuilder(IServiceConfiguration config)
    {
        var scope = ResolveWithConfig<PlanBuilder>(config);
        return scope.Entry;
    }
}
