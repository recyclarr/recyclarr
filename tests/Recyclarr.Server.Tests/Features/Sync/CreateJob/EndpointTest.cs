using System.IO.Abstractions;
using FastEndpoints;
using Recyclarr.Server.Features.Sync.CreateJob;
using Recyclarr.Server.Sync;
using Recyclarr.Server.Tests.Reusable;

namespace Recyclarr.Server.Tests.Features.Sync.CreateJob;

internal sealed class EndpointTest : ServerIntegrationFixture
{
    private void AddInstanceConfig(string instanceName)
    {
        Fs.AddFile(
            Paths.ConfigDirectory.File("recyclarr.yml"),
            new MockFileData(
                $"""
                radarr:
                  {instanceName}:
                    base_url: http://localhost:7878
                    api_key: asdf
                """
            )
        );
    }

    [Test]
    public async Task Requesting_wholly_nonexistent_instance_yields_400_without_creating_job()
    {
        AddInstanceConfig("real-instance");

        var jobStore = Resolve<ISyncJobStore>();
        var ep = Factory.Create<Endpoint>(
            Resolve<ILogger>(),
            Resolve<ServerConfigLoader>(),
            Resolve<SyncJobLauncher>()
        );
        var req = new CreateSyncJobRequest { Instances = ["does-not-exist"] };

        await ep.HandleAsync(req, CancellationToken.None);

        ep.HttpContext.Response.StatusCode.Should().Be(400);
        jobStore.GetAll(null).Should().BeEmpty();
    }

    [Test]
    public async Task Requesting_missing_config_path_yields_400_without_creating_job()
    {
        var missing = Fs.CurrentDirectory().SubDirectory("elsewhere").File("gone.yml");

        var jobStore = Resolve<ISyncJobStore>();
        var ep = Factory.Create<Endpoint>(
            Resolve<ILogger>(),
            Resolve<ServerConfigLoader>(),
            Resolve<SyncJobLauncher>()
        );
        var req = new CreateSyncJobRequest { Configs = [missing.FullName] };

        await ep.HandleAsync(req, CancellationToken.None);

        ep.HttpContext.Response.StatusCode.Should().Be(400);

        jobStore.GetAll(null).Should().BeEmpty();
    }

    [Test]
    public async Task Mixed_valid_and_invalid_instances_yield_400_without_creating_job()
    {
        AddInstanceConfig("real-instance");

        var jobStore = Resolve<ISyncJobStore>();
        var ep = Factory.Create<Endpoint>(
            Resolve<ILogger>(),
            Resolve<ServerConfigLoader>(),
            Resolve<SyncJobLauncher>()
        );

        var req = new CreateSyncJobRequest { Instances = ["real-instance", "does-not-exist"] };

        await ep.HandleAsync(req, CancellationToken.None);

        ep.HttpContext.Response.StatusCode.Should().Be(400);
        jobStore.GetAll(null).Should().BeEmpty();
    }
}
