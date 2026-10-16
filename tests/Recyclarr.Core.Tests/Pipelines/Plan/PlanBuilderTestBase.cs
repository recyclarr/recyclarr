using Autofac;
using Recyclarr.Compatibility;
using Recyclarr.Compatibility.Sonarr;
using Recyclarr.Config.Models;
using Recyclarr.Pipelines.Plan;
using Recyclarr.TrashGuide;

namespace Recyclarr.Core.Tests.Pipelines.Plan;

internal abstract class PlanBuilderTestBase : CoreIntegrationTestFixture
{
    // Planning resolves from the scope created after service detection, so tests supply the
    // detected version (Sonarr defaults to v4, which keeps global quality sizes).
    protected PlanBuilder CreatePlanBuilder(
        IServiceConfiguration config,
        Version? serviceVersion = null
    )
    {
        var detected = config.ServiceType switch
        {
            SupportedServices.Sonarr => SonarrDetected(serviceVersion ?? new Version(4, 0)),
            _ => new DetectedService(
                serviceVersion ?? new Version(5, 0),
                ServiceApiTarget.RadarrV3
            ),
        };

        var scope = ResolveWithConfig<PlanBuilder>(config, c => c.RegisterInstance(detected));
        return scope.Entry;
    }

    private static DetectedService SonarrDetected(Version version) =>
        new(version, new SonarrCapabilities(version).ApiTarget);
}
