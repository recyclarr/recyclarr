using Autofac;
using Recyclarr.TrashLib.TestLibrary;

namespace Recyclarr.Cli.TestLibrary;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public abstract class CliIntegrationFixture : TrashLibIntegrationFixture
{
    protected override void RegisterTypes(ContainerBuilder builder)
    {
        // Do NOT invoke the base method here!
        // We are deliberately REPLACING those registrations (the composition root here is a SUPERSET).
        CompositionRoot.Setup(builder);
    }
}
