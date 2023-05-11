using Autofac;
using Recyclarr.TrashLib.TestLibrary;

namespace Recyclarr.Cli.TestLibrary;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public abstract class CliIntegrationFixture : TrashLibIntegrationFixture
{
    protected override void RegisterTypes(ContainerBuilder builder)
    {
        CompositionRoot.Setup(builder);
    }
}
