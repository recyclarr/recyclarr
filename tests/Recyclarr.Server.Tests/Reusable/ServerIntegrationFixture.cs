using System.IO.Abstractions;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Server.Resources;
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

        // The web host provides IMeterFactory in production; this container has no host.
        var services = new ServiceCollection();
        services.AddMetrics();
        builder.Populate(services);
    }

    /// <summary>
    /// Writes one Radarr instance per name to the config directory and publishes them through the
    /// startup refresh. Each instance gets its own host because instances sharing a base URL are
    /// rejected as split.
    /// </summary>
    protected Task PublishInstances(params string[] instanceNames)
    {
        if (instanceNames.Length > 0)
        {
            var instances = instanceNames.Select(name =>
                $"""
                      {name}:
                        base_url: http://{name}:7878
                        api_key: asdf
                    """
            );
            Fs.AddFile(
                Paths.ConfigDirectory.File("recyclarr.yml"),
                new MockFileData($"radarr:\n{string.Join("\n", instances)}\n")
            );
        }

        return Resolve<ResourceCoordinator>()
            .RefreshAsync(RefreshMode.Startup, CancellationToken.None);
    }
}
