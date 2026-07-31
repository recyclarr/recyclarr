using System.IO.Abstractions;
using System.Text.Json;
using FastEndpoints;
using Recyclarr.Server.Features.Sync.CreateJob;
using Recyclarr.Server.Sync;
using Recyclarr.Server.Tests.Reusable;

namespace Recyclarr.Server.Tests.Features.Sync.CreateJob;

internal sealed class EndpointTest : ServerIntegrationFixture
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

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
    public async Task Requesting_wholly_nonexistent_instance_yields_400_naming_bad_and_available()
    {
        AddInstanceConfig("real-instance");

        var jobStore = Resolve<ISyncJobStore>();
        var ep = Factory.Create<Endpoint>(
            Resolve<ILogger>(),
            Resolve<ServerConfigLoader>(),
            Resolve<SyncJobLauncher>()
        );
        ep.HttpContext.Response.Body = new MemoryStream();

        var req = new CreateSyncJobRequest { Instances = ["does-not-exist"] };

        await ep.HandleAsync(req, CancellationToken.None);

        ep.HttpContext.Response.StatusCode.Should().Be(400);
        ep.HttpContext.Response.ContentType.Should().Be("application/problem+json");

        ep.HttpContext.Response.Body.Position = 0;
        var problem = await JsonSerializer.DeserializeAsync<CreateSyncJobProblemDetails>(
            ep.HttpContext.Response.Body,
            JsonOptions
        );

        problem.Should().NotBeNull();
        problem!.Diagnostics.Should().NotBeNull();
        problem.Diagnostics!.UnknownInstances.Should().Contain("does-not-exist");
        problem.Diagnostics.AvailableInstances.Should().Contain("real-instance");

        jobStore.GetAll(null).Should().BeEmpty();
    }

    [Test]
    public async Task Requesting_a_config_path_that_does_not_exist_yields_400_naming_the_file()
    {
        var missing = Fs.CurrentDirectory().SubDirectory("elsewhere").File("gone.yml");

        var jobStore = Resolve<ISyncJobStore>();
        var ep = Factory.Create<Endpoint>(
            Resolve<ILogger>(),
            Resolve<ServerConfigLoader>(),
            Resolve<SyncJobLauncher>()
        );
        ep.HttpContext.Response.Body = new MemoryStream();

        var req = new CreateSyncJobRequest { Configs = [missing.FullName] };

        await ep.HandleAsync(req, CancellationToken.None);

        ep.HttpContext.Response.StatusCode.Should().Be(400);

        ep.HttpContext.Response.Body.Position = 0;
        var problem = await JsonSerializer.DeserializeAsync<CreateSyncJobProblemDetails>(
            ep.HttpContext.Response.Body,
            JsonOptions
        );

        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Config files not found");
        problem.Diagnostics.Should().NotBeNull();
        problem.Diagnostics!.MissingConfigFiles.Should().Equal(missing.FullName);

        jobStore.GetAll(null).Should().BeEmpty();
    }

    [Test]
    public async Task Mixed_valid_and_invalid_instances_syncs_valid_and_records_invalid_diagnostic()
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

        ep.ValidationFailed.Should().BeFalse();
        ep.Response.Should().NotBeNull();

        var job = jobStore.Get(new JobId { Value = ep.Response!.Id });
        job.Should().NotBeNull();
        job!.ConfigDiagnostics.Should().NotBeNull();
        job.ConfigDiagnostics!.UnknownInstances.Should().Contain("does-not-exist");
        job.ConfigDiagnostics.AvailableInstances.Should().Contain("real-instance");
    }
}
