using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Recyclarr.Client.V1;
using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Server.Sync;
using Recyclarr.Server.TestLibrary;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using Refit;
using CoreService = Recyclarr.TrashGuide.SupportedServices;

namespace Recyclarr.Server.Tests.Http;

internal sealed class SyncJobResultsHttpTest : ServerHttpFixture
{
    [Test]
    public async Task Results_are_unavailable_while_job_is_running()
    {
        var store = Services.GetRequiredService<ISyncJobStore>();
        var job = store.Create(new ServerSyncSettings(null, [], Preview: false, []));
        store.Update(job.Id, x => x.Status = SyncJobStatus.Running);

        using var client = CreateClient();
        var uri = new Uri($"/api/v1/sync/jobs/{job.Id.Value}/results", UriKind.Relative);
        var response = await client.GetAsync(uri);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Test]
    public async Task Generated_client_reads_the_service_specific_instance_result()
    {
        var noOp = new CustomFormatPipelineResult(0, 0, [], []);
        var result = new SyncRunResult([new SyncInstanceResult("tv", CoreService.Sonarr, [noOp])]);
        var job = CreateCompletedJob(result);

        using var client = CreateClient();
        var api = RestService.For<ISyncApi>(client);
        var response = await api.Results(job.Id.Value);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Error.Should().BeNull();
        response.Content.Should().NotBeNull();
        response.Content.Status.Should().Be(SyncCompletionStatus.Succeeded);
        var instance = response
            .Content.Instances.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<SyncInstanceResultsResponseSonarrInstanceResultsResponse>()
            .Which;
        instance.Name.Should().Be("tv");
        instance.Pipelines.CustomFormats?.Status.Should().Be(PipelineStatus.Succeeded);
        instance.Pipelines.CustomFormats?.Creates.Should().BeEmpty();
    }

    [Test]
    public async Task Completed_job_can_return_no_instances()
    {
        var job = CreateCompletedJob(new SyncRunResult([]));

        using var client = CreateClient();
        var response = await RestService.For<ISyncApi>(client).Results(job.Id.Value);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content?.Status.Should().Be(SyncCompletionStatus.Succeeded);
        response.Content?.Instances.Should().BeEmpty();
    }

    [Test]
    public async Task Generated_client_reads_every_pipeline_contract()
    {
        var job = CreateCompletedJob(SyncResultScenarios.CompletePipelineContract());

        using var client = CreateClient();
        var response = await RestService.For<ISyncApi>(client).Results(job.Id.Value);

        response.Error.Should().BeNull();
        response.Content.Should().NotBeNull();
        response.Content.Status.Should().Be(SyncCompletionStatus.Partial);
        response.Content.Instances.Should().HaveCount(2);

        var sonarr = response
            .Content.Instances.Should()
            .ContainSingle(x => x is SyncInstanceResultsResponseSonarrInstanceResultsResponse)
            .Which.Should()
            .BeOfType<SyncInstanceResultsResponseSonarrInstanceResultsResponse>()
            .Which;
        AssertCustomFormats(sonarr.Pipelines.CustomFormats);
        AssertQualityProfiles(sonarr.Pipelines.QualityProfiles);
        AssertSonarrNaming(sonarr.Pipelines.Naming);
        sonarr.Pipelines.MediaManagement?.Updates.Should().ContainSingle();

        var radarr = response
            .Content.Instances.Should()
            .ContainSingle(x => x is SyncInstanceResultsResponseRadarrInstanceResultsResponse)
            .Which.Should()
            .BeOfType<SyncInstanceResultsResponseRadarrInstanceResultsResponse>()
            .Which;
        AssertQualitySizes(radarr.Pipelines.QualitySizes);
        AssertRadarrNaming(radarr.Pipelines.Naming);

        var raw = await client.GetStringAsync(
            new Uri($"/api/v1/sync/jobs/{job.Id.Value}/results", UriKind.Relative)
        );
        raw.Should().Contain("\"service\":\"sonarr\"");
        raw.Should().Contain("\"upgradeAllowed\":null");
        raw.Should().Contain("\"current\":null").And.Contain("\"desired\":false");
        raw.Should().NotContain("sonarrNaming").And.NotContain("radarrNaming");
    }

    [Test]
    public async Task Unknown_job_results_yield_problem_details()
    {
        using var client = CreateClient();
        var uri = new Uri($"/api/v1/sync/jobs/{Guid.NewGuid()}/results", UriKind.Relative);
        var response = await client.GetAsync(uri);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Test]
    public async Task Finished_job_without_a_result_returns_safe_internal_error()
    {
        var store = Services.GetRequiredService<ISyncJobStore>();
        var job = store.Create(new ServerSyncSettings(null, [], Preview: false, []));
        store.Update(job.Id, x => x.Status = SyncJobStatus.Succeeded);

        using var client = CreateClient();
        var uri = new Uri($"/api/v1/sync/jobs/{job.Id.Value}/results", UriKind.Relative);
        var response = await client.GetAsync(uri);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Test]
    public async Task Blocked_pipeline_identifies_its_failed_dependency()
    {
        var customFormats = new CustomFormatPipelineResult(
            completedResources: 0,
            incompleteResources: 1,
            [new CustomFormatCreateRejectedOutcome(new CustomFormatIdentity("cf-one", "CF"))],
            []
        );
        var qualityProfiles = new QualityProfilePipelineResult(0, 0, [], []).WithStatus(
            SyncResultStatus.Blocked,
            PipelineType.CustomFormat
        );
        var result = new SyncRunResult([
            new SyncInstanceResult("tv", CoreService.Sonarr, [customFormats, qualityProfiles]),
        ]);
        var job = CreateCompletedJob(result);

        using var client = CreateClient();
        var response = await RestService.For<ISyncApi>(client).Results(job.Id.Value);

        var content = response.Content ?? throw new InvalidOperationException("Missing response");
        var instance = content
            .Instances.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<SyncInstanceResultsResponseSonarrInstanceResultsResponse>()
            .Which;
        instance.Pipelines.CustomFormats?.Status.Should().Be(PipelineStatus.Failed);
        instance.Pipelines.QualityProfiles?.Status.Should().Be(PipelineStatus.Blocked);
        instance.Pipelines.QualityProfiles?.BlockedBy.Should().Be(BlockingPipeline.CustomFormat);
    }

    [Test]
    public async Task Instance_failures_and_job_fault_keep_their_scopes()
    {
        OperationalFailure[] failures =
        [
            new ServiceUnavailableFailure(),
            new ServiceUnauthenticatedFailure(),
            new ServiceUnauthorizedFailure(),
            new ServiceRateLimitedFailure(),
            new ServiceIncompatibleFailure(),
            new SyncStateUnavailableFailure(),
        ];
        var instances = failures
            .Select(
                (failure, index) =>
                    new SyncInstanceResult(
                        $"instance-{index}",
                        index % 2 == 0 ? CoreService.Sonarr : CoreService.Radarr,
                        [],
                        failure
                    )
            )
            .ToList();
        var job = CreateCompletedJob(new SyncRunResult(instances, new SyncFault("fault-ref")));

        using var client = CreateClient();
        var response = await RestService.For<ISyncApi>(client).Results(job.Id.Value);

        var content = response.Content ?? throw new InvalidOperationException("Missing response");
        content.Status.Should().Be(SyncCompletionStatus.Failed);
        content.Fault?.Reference.Should().Be("fault-ref");
        content
            .Instances.Select(x =>
                x switch
                {
                    SyncInstanceResultsResponseSonarrInstanceResultsResponse sonarr =>
                        sonarr.Failure,
                    SyncInstanceResultsResponseRadarrInstanceResultsResponse radarr =>
                        radarr.Failure,
                    _ => throw new ArgumentOutOfRangeException(nameof(x)),
                }
            )
            .Should()
            .Equal(
                InstanceFailureCategory.ServiceUnavailable,
                InstanceFailureCategory.ServiceUnauthenticated,
                InstanceFailureCategory.ServiceUnauthorized,
                InstanceFailureCategory.ServiceRateLimited,
                InstanceFailureCategory.ServiceIncompatible,
                InstanceFailureCategory.SyncStateUnavailable
            );
    }

    [Test]
    public async Task Evicted_job_results_return_not_found()
    {
        SyncJob? first = null;
        for (var i = 0; i < 51; i++)
        {
            var job = CreateCompletedJob(new SyncRunResult([]));
            first ??= job;
        }

        using var client = CreateClient();
        var firstId = first?.Id.Value ?? throw new InvalidOperationException("No job created");
        var uri = new Uri($"/api/v1/sync/jobs/{firstId}/results", UriKind.Relative);
        var response = await client.GetAsync(uri);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private SyncJob CreateCompletedJob(SyncRunResult result)
    {
        var store = Services.GetRequiredService<ISyncJobStore>();
        var job = store.Create(new ServerSyncSettings(null, [], Preview: false, []));
        store.Update(
            job.Id,
            x =>
            {
                x.Result = result;
                x.Status = result.Status.ToJobStatus();
            }
        );
        return job;
    }

    private static void AssertCustomFormats(CustomFormatPipelineResponse? pipeline)
    {
        pipeline.Should().NotBeNull();
        pipeline.Creates.Should().ContainSingle();
        pipeline.Updates.Should().ContainSingle();
        pipeline.Deletes.Should().ContainSingle();
        var outcomes = pipeline.Outcomes;
        int?[] counts =
        [
            outcomes.ReferenceMismatches?.Count,
            outcomes.GroupReferenceMismatches?.Count,
            outcomes.IncompatibleGroups?.Count,
            outcomes.EmptyGroups?.Count,
            outcomes.Adopted?.Count,
            outcomes.AmbiguousMatches?.Count,
            outcomes.StateConflicts?.Count,
            outcomes.CreateRejected?.Count,
            outcomes.UpdateRejected?.Count,
            outcomes.DeleteRejected?.Count,
        ];
        counts.Should().HaveCount(10).And.OnlyContain(x => x == 1);
    }

    private static void AssertQualityProfiles(QualityProfilePipelineResponse? pipeline)
    {
        pipeline.Should().NotBeNull();
        pipeline.Creates.Should().ContainSingle();
        pipeline.Updates.Should().ContainSingle();
        var outcomes = pipeline.Outcomes;
        int?[] counts =
        [
            outcomes.ReferenceMismatches?.Count,
            outcomes.DuplicateNames?.Count,
            outcomes.ScoreCollisions?.Count,
            outcomes.NotFound?.Count,
            outcomes.Adopted?.Count,
            outcomes.MinimumScoresUnsatisfied?.Count,
            outcomes.InvalidCutoffs?.Count,
            outcomes.UnavailableCutoffs?.Count,
            outcomes.QualitiesRequired?.Count,
            outcomes.QualityReferenceMismatches?.Count,
            outcomes.ResetScoreReferenceMismatches?.Count,
            outcomes.RenameBlocked?.Count,
            outcomes.AmbiguousMatches?.Count,
            outcomes.CreateRejected?.Count,
            outcomes.UpdateRejected?.Count,
        ];
        counts.Should().HaveCount(15).And.OnlyContain(x => x == 1);

        var update = pipeline.Updates.Should().ContainSingle().Which;
        object?[] changes =
        [
            update.Name,
            update.UpgradeAllowed,
            update.UpgradeUntilQuality,
            update.UpgradeUntilScore,
            update.MinimumFormatScore,
            update.MinimumUpgradeFormatScore,
            update.Language,
            update.QualityLayout,
        ];
        changes.Should().OnlyContain(x => x != null);
        update.CustomFormatScores.Should().HaveCount(2);
    }

    private static void AssertQualitySizes(QualitySizePipelineResponse? pipeline)
    {
        pipeline.Should().NotBeNull();
        var outcomes = pipeline.Outcomes;
        int?[] counts =
        [
            outcomes.DefinitionReferenceMismatches?.Count,
            outcomes.ReferenceMismatches?.Count,
            outcomes.ServiceQualitiesNotFound?.Count,
            outcomes.PreferredRatiosClamped?.Count,
            outcomes.MinimumGreaterThanPreferred?.Count,
            outcomes.UnlimitedPreferredGreaterThanMaximum?.Count,
            outcomes.PreferredGreaterThanMaximum?.Count,
        ];
        counts.Should().HaveCount(7).And.OnlyContain(x => x == 1);

        var update = pipeline.Updates.Should().ContainSingle().Which;
        update.Minimum.Should().NotBeNull();
        update.Preferred.Should().NotBeNull();
        update.Maximum.Should().NotBeNull();
    }

    private static void AssertSonarrNaming(SonarrNamingPipelineResponse? pipeline)
    {
        pipeline?.Outcomes.ReferenceMismatches.Should().HaveCount(5);
        var update = pipeline?.Updates.Should().ContainSingle().Which;
        object?[] changes =
        [
            update?.RenameEpisodes,
            update?.SeriesFolderFormat,
            update?.SeasonFolderFormat,
            update?.StandardEpisodeFormat,
            update?.DailyEpisodeFormat,
            update?.AnimeEpisodeFormat,
        ];
        changes.Should().HaveCount(6).And.OnlyContain(x => x != null);
    }

    private static void AssertRadarrNaming(RadarrNamingPipelineResponse? pipeline)
    {
        pipeline?.Outcomes.ReferenceMismatches.Should().HaveCount(2);
        var update = pipeline?.Updates.Should().ContainSingle().Which;
        update?.RenameMovies.Should().NotBeNull();
        update?.StandardMovieFormat.Should().NotBeNull();
        update?.MovieFolderFormat.Should().NotBeNull();
    }
}
