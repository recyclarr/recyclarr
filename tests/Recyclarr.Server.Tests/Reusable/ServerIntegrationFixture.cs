using Autofac;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Server.TestLibrary;

namespace Recyclarr.Server.Tests.Reusable;

// Server-side equivalent of Recyclarr.Cli.Tests' CliIntegrationFixture. Wires up the real Server
// composition root against the standard test stubs, plus the sync engine stubs, so tests that
// exercise HandleAsync directly don't trigger real network-bound sync pipelines.
internal abstract class ServerIntegrationFixture : IntegrationTestFixture
{
    protected override void RegisterTypes(ContainerBuilder builder)
    {
        // Do NOT invoke the base method here! The composition root here is a SUPERSET.
        CompositionRoot.Setup(builder);
    }

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        base.RegisterStubsAndMocks(builder);
        builder.RegisterModule<ServerSyncStubsModule>();
    }
}
