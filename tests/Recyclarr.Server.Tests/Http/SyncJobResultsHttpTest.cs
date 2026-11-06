using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Recyclarr.Client.V1;
using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Server.Sync;
using Recyclarr.Server.TestLibrary;
using Recyclarr.Server.Tests.Reusable;
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
        var job = store.CreateManualJob([]);
        store.Update(job.Id, x => x.Status = SyncJobStatus.Running);

        using var client = CreateClient();
        var uri = new Uri($"/api/v1/sync/jobs/{job.Id.Value}/results", UriKind.Relative);
        var response = await client.GetAsync(uri);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Test]
    public async Task Skipped_job_has_no_results()
    {
        var store = Services.GetRequiredService<ISyncJobStore>();
        var settings = new ServerSyncSettings(null, [], Preview: false);
        store.CreateManualJob([], settings);
        var skipped = store.CreateScheduled(settings, [], DateTimeOffset.UnixEpoch);

        using var client = CreateClient();
        var uri = new Uri($"/api/v1/sync/jobs/{skipped.Id.Value}/results", UriKind.Relative);
        var response = await client.GetAsync(uri);

        skipped.Status.Should().Be(SyncJobStatus.Skipped);
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
            .BeOfType<SyncInstanceResultsResponseSonarr>()
            .Which;
        instance.Name.Should().Be("tv");
        instance.Pipelines.CustomFormats?.Status.Should().Be(PipelineStatus.Succeeded);
        instance.Pipelines.CustomFormats?.Creates.Should().BeEmpty();
    }

    [Test]
    public async Task Instance_fault_retains_its_scope_and_buffered_semantic_data()
    {
        var noOp = new CustomFormatPipelineResult(0, 0, [], []);
        var result = new SyncRunResult([
            new SyncInstanceResult(
                "faulted",
                CoreService.Sonarr,
                [noOp],
                fault: new SyncFault("instance-fault")
            ),
            new SyncInstanceResult("later", CoreService.Radarr, []),
        ]);
        var job = CreateCompletedJob(result);

        using var client = CreateClient();
        var response = await RestService.For<ISyncApi>(client).Results(job.Id.Value);

        var content = response.Content ?? throw new InvalidOperationException("Missing response");
        content.Status.Should().Be(SyncCompletionStatus.Partial);
        content.Fault.Should().BeNull();
        var faulted = content
            .Instances[0]
            .Should()
            .BeOfType<SyncInstanceResultsResponseSonarr>()
            .Which;
        faulted.Name.Should().Be("faulted");
        faulted.Status.Should().Be(SyncCompletionStatus.Partial);
        faulted.Fault?.Reference.Should().Be("instance-fault");
        faulted.Pipelines.CustomFormats?.Status.Should().Be(PipelineStatus.Succeeded);
        content
            .Instances[1]
            .Should()
            .BeOfType<SyncInstanceResultsResponseRadarr>()
            .Which.Name.Should()
            .Be("later");
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
            .ContainSingle(x => x is SyncInstanceResultsResponseSonarr)
            .Which.Should()
            .BeOfType<SyncInstanceResultsResponseSonarr>()
            .Which;
        AssertCustomFormats(sonarr.Pipelines.CustomFormats);
        AssertSonarrQualityProfiles(sonarr.Pipelines.QualityProfiles);
        AssertSonarrNaming(sonarr.Pipelines.Naming);
        sonarr.Pipelines.MediaManagement?.Updates.Should().ContainSingle();

        var radarr = response
            .Content.Instances.Should()
            .ContainSingle(x => x is SyncInstanceResultsResponseRadarr)
            .Which.Should()
            .BeOfType<SyncInstanceResultsResponseRadarr>()
            .Which;
        AssertRadarrQualityProfiles(radarr.Pipelines.QualityProfiles);
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
    public async Task Results_expose_every_planning_outcome_variant_in_order()
    {
        var contract = SyncResultScenarios.CompletePlanningOutcomeContract();
        AssertCompletePlanningOutcomeInventory(contract);
        var job = CreateCompletedJob(contract);

        using var client = CreateClient();
        var response = await RestService.For<ISyncApi>(client).Results(job.Id.Value);
        response.Error.Should().BeNull();
        response.Content.Should().NotBeNull();
        var instance = response
            .Content.Instances.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<SyncInstanceResultsResponseSonarr>()
            .Which;
        var planningOutcomes = instance.PlanningOutcomes;
        planningOutcomes.Should().NotBeNull();
        planningOutcomes
            .Select(x => (x.Code, x.Severity, x.Blocking))
            .Should()
            .Equal(
                ("customFormatReferenceMismatch", OutcomeSeverity.Error, true),
                ("customFormatGroupReferenceMismatch", OutcomeSeverity.Error, true),
                ("customFormatGroupSkipReferenceMismatch", OutcomeSeverity.Error, true),
                ("customFormatGroupSelectReferenceMismatch", OutcomeSeverity.Error, true),
                ("customFormatGroupExcludeReferenceMismatch", OutcomeSeverity.Error, true),
                ("customFormatGroupQualityProfileReferenceMismatch", OutcomeSeverity.Error, true),
                ("customFormatQualityProfileReferenceAmbiguous", OutcomeSeverity.Error, true),
                ("customFormatGroupQualityProfileReferenceAmbiguous", OutcomeSeverity.Error, true),
                ("customFormatGroupRequiredItemSelected", OutcomeSeverity.Info, false),
                ("customFormatGroupDefaultItemSelected", OutcomeSeverity.Info, false),
                ("customFormatGroupRequiredItemExcluded", OutcomeSeverity.Warning, false),
                ("customFormatGroupNonDefaultItemExcluded", OutcomeSeverity.Warning, false)
            );
        planningOutcomes.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.Message));

        var ambiguity = planningOutcomes[7];
        ambiguity
            .Subject.Should()
            .BeEquivalentTo(new { Kind = "customFormatGroup", TrashId = "ambiguous-group" });
        DataAs<QualityProfileReferenceAmbiguousOutcomeData>(ambiguity)
            .Should()
            .BeEquivalentTo(
                new
                {
                    ProfileTrashId = "group-profile",
                    ProfileNames = new[] { "Group A", "Group B" },
                }
            );
    }

    // Outcome data is untyped in the envelope; clients convert it to the class the spec maps the
    // code to.
    private static T DataAs<T>(OutcomeResponse outcome)
    {
        var json = outcome.Data.Should().BeOfType<JsonElement>().Which;
        return json.Deserialize<T>(DataJson)
            ?? throw new InvalidOperationException("Outcome data is null");
    }

    private static readonly JsonSerializerOptions DataJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static void AssertCompletePlanningOutcomeInventory(SyncRunResult contract)
    {
        var representedTypes = contract
            .Instances.SelectMany(x => x.PlanningOutcomes)
            .Select(x => x.GetType());
        var concreteContractTypes = typeof(PlanningOutcome)
            .Assembly.GetTypes()
            .Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(PlanningOutcome)));

        representedTypes.Should().BeEquivalentTo(concreteContractTypes);
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
            .BeOfType<SyncInstanceResultsResponseSonarr>()
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
            new ServiceRedirectedFailure(),
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
                    SyncInstanceResultsResponseSonarr sonarr => sonarr.Failure,
                    SyncInstanceResultsResponseRadarr radarr => radarr.Failure,
                    _ => throw new ArgumentOutOfRangeException(nameof(x)),
                }
            )
            .Should()
            .Equal(
                InstanceFailureCategory.ServiceUnavailable,
                InstanceFailureCategory.ServiceUnauthenticated,
                InstanceFailureCategory.ServiceUnauthorized,
                InstanceFailureCategory.ServiceRateLimited,
                InstanceFailureCategory.ServiceRedirected,
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
        var job = store.CreateManualJob(result.Instances.Select(x => x.InstanceName).ToList());
        store.Update(job.Id, x => x.Status = SyncJobStatus.Running);
        Services.GetRequiredService<SyncJobFinalizer>().Complete(job.Id, result);
        return job;
    }

    private static void AssertCustomFormats(CustomFormatPipelineResponse? pipeline)
    {
        pipeline.Should().NotBeNull();
        pipeline.Creates.Should().ContainSingle();
        pipeline.Updates.Should().ContainSingle();
        pipeline.Deletes.Should().ContainSingle();
        pipeline
            .Outcomes.Select(x => x.Code)
            .Should()
            .Equal(
                "customFormatGroupIncompatible",
                "customFormatGroupEmpty",
                "customFormatDuplicateName",
                "customFormatAdopted",
                "customFormatAmbiguousMatch",
                "customFormatStateConflict",
                "customFormatCreateRejected",
                "customFormatUpdateRejected",
                "customFormatDeleteRejected"
            );
        var adopted = pipeline.Outcomes[3];
        adopted.Severity.Should().Be(OutcomeSeverity.Info);
        adopted
            .Subject.Should()
            .BeEquivalentTo(
                new
                {
                    Kind = "customFormat",
                    TrashId = "cf-one",
                    Name = "First CF",
                }
            );
        DataAs<ServiceIdOutcomeData>(adopted).ServiceId.Should().Be(10);
    }

    private static void AssertSonarrQualityProfiles(SonarrQualityProfilePipelineResponse? pipeline)
    {
        pipeline.Should().NotBeNull();
        pipeline
            .Creates.Should()
            .ContainSingle()
            .Which.State.QualitySizes.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new
                {
                    Quality = "HDTV-1080p",
                    Minimum = new { Kind = QualitySizeKind.Numeric, Value = 5m },
                    Maximum = new { Kind = QualitySizeKind.Unlimited },
                }
            );
        AssertQualityProfileOutcomes(
            pipeline.Outcomes,
            "qualitySizeGuideReferenceMismatch",
            "qualityProfileQualityDefinitionUnused"
        );
        var sizeOutcome = pipeline.Outcomes.Single(x =>
            x.Code == "qualitySizeGuideReferenceMismatch"
        );
        sizeOutcome.Subject?.TrashId.Should().Be("qp-one");
        DataAs<QualitySizeGuideReferenceOutcomeData>(sizeOutcome).TrashId.Should().Be("qs-missing");

        var update = pipeline.Updates.Should().ContainSingle().Which;
        update
            .QualitySizes.Should()
            .ContainSingle()
            .Which.Minimum.Should()
            .BeEquivalentTo(new { Current = new { Value = 1m }, Desired = new { Value = 5m } });
        object?[] changes =
        [
            update.Name,
            update.UpgradeAllowed,
            update.UpgradeUntilQuality,
            update.UpgradeUntilScore,
            update.MinimumFormatScore,
            update.MinimumUpgradeFormatScore,
            update.QualityLayout,
        ];
        changes.Should().OnlyContain(x => x != null);
        update.CustomFormatScores.Should().HaveCount(2);
    }

    private static void AssertRadarrQualityProfiles(RadarrQualityProfilePipelineResponse? pipeline)
    {
        pipeline.Should().NotBeNull();
        pipeline.Creates.Should().ContainSingle().Which.State.Language.Should().Be("English");
        AssertQualityProfileOutcomes(pipeline.Outcomes, "qualityProfileQualityDefinitionIgnored");

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

    private static void AssertQualityProfileOutcomes(
        ICollection<OutcomeResponse> outcomes,
        params string[] serviceCodes
    )
    {
        outcomes
            .Select(x => x.Code)
            .Should()
            .Equal([
                "qualityProfileReferenceMismatch",
                "qualityProfileDuplicateName",
                "qualityProfileNotFound",
                "qualityProfileAdopted",
                "qualityProfileMinimumScoreUnsatisfied",
                "qualityProfileInvalidCutoff",
                "qualityProfileUnavailableCutoff",
                "qualityProfileQualitiesRequired",
                "qualityProfileQualityReferenceMismatch",
                "qualityProfileResetScoreReferenceMismatch",
                "qualityProfileRenameBlocked",
                "qualityProfileAmbiguousMatch",
                "qualityProfileCreateRejected",
                "qualityProfileUpdateRejected",
                .. serviceCodes,
            ]);
    }

    private static void AssertQualitySizes(QualitySizePipelineResponse? pipeline)
    {
        pipeline.Should().NotBeNull();
        pipeline
            .Outcomes.Select(x => x.Code)
            .Should()
            .Equal(
                "qualitySizeDefinitionReferenceMismatch",
                "qualitySizeReferenceMismatch",
                "qualitySizeServiceQualityNotFound",
                "qualitySizePreferredRatioClamped",
                "qualitySizeMinimumGreaterThanPreferred",
                "qualitySizeUnlimitedPreferredGreaterThanMaximum",
                "qualitySizePreferredGreaterThanMaximum",
                "qualitySizeGuideReferenceMismatch",
                "qualitySizeAmbiguousGuideReference"
            );
        var minimum = DataAs<MinimumComparisonOutcomeData>(pipeline.Outcomes.ElementAt(4));
        minimum.Minimum.Value.Should().Be(2.5);
        minimum.Preferred.Value.Should().Be(1.25);

        var update = pipeline.Updates.Should().ContainSingle().Which;
        update.Minimum.Should().NotBeNull();
        update.Preferred.Should().NotBeNull();
        update.Maximum.Should().NotBeNull();
    }

    private static void AssertSonarrNaming(SonarrNamingPipelineResponse? pipeline)
    {
        pipeline?.Outcomes.Should().HaveCount(5);
        DataAs<MediaNamingReferenceOutcomeData>(pipeline!.Outcomes.First())
            .Field.Should()
            .Be("seriesFolderFormat");
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
        pipeline?.Outcomes.Should().HaveCount(2);
        var update = pipeline?.Updates.Should().ContainSingle().Which;
        update?.RenameMovies.Should().NotBeNull();
        update?.StandardMovieFormat.Should().NotBeNull();
        update?.MovieFolderFormat.Should().NotBeNull();
    }
}
