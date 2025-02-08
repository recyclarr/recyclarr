using Autofac;
using Recyclarr.TestLibrary;

namespace Recyclarr.Cli.IntegrationTests;

internal abstract class CliIntegrationFixture : IntegrationTestFixture
{
    protected override void RegisterTypes(ContainerBuilder builder)
    {
        // Do NOT invoke the base method here!
        // We are deliberately REPLACING those registrations (the composition root here is a SUPERSET).
        CompositionRoot.Setup(builder);
    }
}
