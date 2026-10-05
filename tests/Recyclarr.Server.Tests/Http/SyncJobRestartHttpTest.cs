using System.IO.Abstractions;
using System.Net;
using Autofac;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Recyclarr.Server.Persistence;
using Recyclarr.Server.Sync;
using Recyclarr.Server.Sync.Results;
using Recyclarr.Server.TestLibrary;
using Recyclarr.Sync.Results;
using Recyclarr.TrashGuide;
using GeneratedProgressStatus = Recyclarr.Client.V1.InstanceProgressStatusResponse;
using ISyncApi = Recyclarr.Client.V1.ISyncApi;
using RestService = Refit.RestService;

namespace Recyclarr.Server.Tests.Http;

// Each test starts one server, stops it, and starts a second server on the same database file.
internal sealed class SyncJobRestartHttpTest
{
    private string _databasePath = "";

    [SetUp]
    public void CreateDatabasePath()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"recyclarr-{Guid.NewGuid():N}.db");
    }

    [TearDown]
    public void DeleteDatabase()
    {
        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            File.Delete(_databasePath + suffix);
        }
    }

    [Test]
    public async Task Finished_job_and_its_results_survive_a_restart()
    {
        JobId id;
        await using (var first = new FileDatabaseServer(_databasePath))
        {
            using var _ = first.CreateClient();
            var store = first.Services.GetRequiredService<ISyncJobStore>();
            id = store.Create(new ServerSyncSettings(null, [], Preview: false), ["movies"]).Id;
            store.Update(id, job => job.Status = SyncJobStatus.Running);
            first
                .Services.GetRequiredService<SyncJobFinalizer>()
                .Complete(
                    id,
                    new SyncRunResult([
                        new SyncInstanceResult("movies", SupportedServices.Radarr, []),
                    ])
                );
        }

        await using var second = new FileDatabaseServer(_databasePath);
        using var client = second.CreateClient();
        var api = RestService.For<ISyncApi>(client);

        var job = await api.JobsGet(id.Value);
        var results = await api.Results(id.Value);

        job.StatusCode.Should().Be(HttpStatusCode.OK);
        job.Content?.Status.Should().Be(nameof(SyncJobStatus.Succeeded));
        results
            .Content?.Instances.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<Recyclarr.Client.V1.SyncInstanceResultsResponseRadarr>()
            .Which.Name.Should()
            .Be("movies");
    }

    [Test]
    public async Task Job_active_at_shutdown_is_interrupted_after_restart()
    {
        JobId id;
        await using (var first = new FileDatabaseServer(_databasePath))
        {
            using var _ = first.CreateClient();
            var store = first.Services.GetRequiredService<ISyncJobStore>();
            id = store
                .Create(
                    new ServerSyncSettings(null, [], Preview: false),
                    ["finished", "running", "waiting"]
                )
                .Id;
            var finished = SyncJobResultsResponseMapper.MapInstance(
                new SyncInstanceResult("finished", SupportedServices.Radarr, [])
            );
            store.Update(
                id,
                job =>
                {
                    job.Status = SyncJobStatus.Running;
                    job.Progress = job
                        .Progress.Start("finished")
                        .Complete(finished)
                        .Start("running");
                }
            );
        }

        await using var second = new FileDatabaseServer(_databasePath);
        using var client = second.CreateClient();
        var api = RestService.For<ISyncApi>(client);

        var job = await api.JobsGet(id.Value);
        var results = await api.Results(id.Value);

        job.Content?.Status.Should().Be(nameof(SyncJobStatus.Interrupted));
        job.Content?.FinishedAt.Should().NotBeNull();
        job.Content?.Progress.Select(x => x.Status)
            .Should()
            .Equal(
                GeneratedProgressStatus.Succeeded,
                GeneratedProgressStatus.Interrupted,
                GeneratedProgressStatus.NotRun
            );
        results.StatusCode.Should().Be(HttpStatusCode.OK);
        results
            .Content?.Instances.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<Recyclarr.Client.V1.SyncInstanceResultsResponseRadarr>()
            .Which.Name.Should()
            .Be("finished");
    }

    private sealed class FileDatabaseServer(string path) : ServerHttpFixture
    {
        protected override void RegisterStubsAndMocks(ContainerBuilder builder)
        {
            builder
                .Register(_ => ServerDatabase.File(new FileSystem().FileInfo.New(path)))
                .SingleInstance();
        }
    }
}
