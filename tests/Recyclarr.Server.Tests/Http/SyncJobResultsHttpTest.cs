using System.IO.Abstractions;
using System.Net;
using System.Net.Http.Json;
using Autofac;
using FastEndpoints;
using Recyclarr.Config.Models;
using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Pipelines.CustomFormat.Models;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Pipelines.QualityProfile.Models;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Servarr.MediaManagement;
using Recyclarr.Servarr.MediaNaming;
using Recyclarr.Servarr.QualityProfile;
using Recyclarr.Servarr.QualitySize;
using Recyclarr.Server.Features.Sync.CreateJob;
using Recyclarr.Server.Features.Sync.GetJobResults;
using Recyclarr.Server.TestLibrary;
using Recyclarr.Sync;
using Recyclarr.SyncState;
using Recyclarr.TestLibrary.Autofac;
using Recyclarr.TrashGuide.QualitySize;
using CreateJobEndpoint = Recyclarr.Server.Features.Sync.CreateJob.Endpoint;

namespace Recyclarr.Server.Tests.Http;

// Exercises GET /sync/jobs/{id}/results end to end: a real sync job is created over HTTP, the
// runner captures results out of the (stubbed) sync scope before it is disposed, and the
// sub-resource serves them as wire JSON.
internal sealed class SyncJobResultsHttpTest : ServerHttpFixture
{
    // Read lazily by the ISyncRunResults substitute, so each test can decide what the sync run
    // "produced" before it triggers a job.
    private SyncInstanceResult _instanceResult = new();

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        builder.RegisterMockFor<ISyncRunResults>(m =>
            m.GetInstanceResult(default!).ReturnsForAnyArgs(_ => _instanceResult)
        );
    }

    [Test]
    public async Task Unknown_job_id_yields_problem_details()
    {
        using var client = CreateClient();

        var response = await client.GetAsync(
            new Uri($"/api/v1/sync/jobs/{Guid.NewGuid()}/results", UriKind.Relative)
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Test]
    public async Task Operations_that_produced_nothing_are_absent_from_the_wire()
    {
        _instanceResult = new SyncInstanceResult();

        using var client = CreateClient();
        var results = await RunJobAndFetchResults(client);

        var instance = results.Instances.Should().ContainSingle().Subject;
        instance.Instance.Should().Be("real-instance");
        instance.CustomFormats.Should().BeNull();
        instance.QualityProfiles.Should().BeNull();
        instance.QualitySizes.Should().BeNull();
        instance.SonarrNaming.Should().BeNull();
        instance.RadarrNaming.Should().BeNull();
        instance.MediaManagement.Should().BeNull();
    }

    [Test]
    public async Task All_six_operations_are_served_for_the_instance()
    {
        _instanceResult = BuildFullResult();

        using var client = CreateClient();
        var results = await RunJobAndFetchResults(client);

        var instance = results.Instances.Should().ContainSingle().Subject;

        instance.CustomFormats!.UnchangedCount.Should().Be(1);
        instance
            .CustomFormats.Changes.Should()
            .BeEquivalentTo([
                new CustomFormatChangeResponse("Create", "New CF", "trash1")
                {
                    Source = new CustomFormatSourceResponse("CfGroupImplicit")
                    {
                        GroupName = "My Group",
                        ProfileNames = ["profile-a"],
                    },
                    InclusionReason = "Required",
                },
                // FlatConfig with no inclusion reason: the reason is omitted, the source is not
                new CustomFormatChangeResponse("Update", "Updated CF", "trash2")
                {
                    Source = new CustomFormatSourceResponse("FlatConfig"),
                },
                // No source info recorded at all
                new CustomFormatChangeResponse("Delete", "Deleted CF", "trash3"),
            ]);

        var profile = instance.QualityProfiles!.Profiles.Should().ContainSingle().Subject;
        profile.Name.Should().Be("HD Bluray");
        profile.ChangeReason.Should().Be("Changed");
        profile.Current.MinFormatScore.Should().Be(50);
        profile.Desired.MinFormatScore.Should().Be(75);

        // No `qualities` in config means there is nothing to render side by side.
        profile.Qualities.Should().BeNull();

        // The NoChange score and the one whose value did not move are both dropped.
        profile
            .ScoreChanges.Should()
            .BeEquivalentTo([new FormatScoreChangeResponse("Moved CF", 100, 200, "Updated")]);

        // Only the differing size is projected; the unchanged one is dropped.
        instance
            .QualitySizes.Should()
            .BeEquivalentTo(
                new QualitySizesResultResponse
                {
                    Items = [new QualitySizeItemResponse("Bluray-1080p", 5, 100, 95)],
                    MaxLimit = 400,
                    PreferredLimit = 395,
                }
            );

        instance
            .SonarrNaming.Should()
            .BeEquivalentTo(
                new SonarrNamingResultResponse
                {
                    RenameEpisodes = true,
                    SeriesFolderFormat = "desired-series",
                    SeasonFolderFormat = "desired-season",
                    StandardEpisodeFormat = "desired-standard",
                    DailyEpisodeFormat = "desired-daily",
                    AnimeEpisodeFormat = "desired-anime",
                }
            );

        instance
            .RadarrNaming.Should()
            .BeEquivalentTo(
                new RadarrNamingResultResponse
                {
                    RenameMovies = true,
                    StandardMovieFormat = "desired-movie",
                    MovieFolderFormat = "desired-folder",
                }
            );

        instance.MediaManagement!.PropersAndRepacks.Should().Be("DoNotUpgrade");
    }

    private async Task<GetSyncJobResultsResponse> RunJobAndFetchResults(HttpClient client)
    {
        Fs.AddFile(
            Paths.ConfigDirectory.File("recyclarr.yml"),
            new MockFileData(
                """
                radarr:
                  real-instance:
                    base_url: http://localhost:7878
                    api_key: asdf
                """
            )
        );

        var (_, created) = await client.POSTAsync<
            CreateJobEndpoint,
            CreateSyncJobRequest,
            CreateSyncJobResponse
        >(new CreateSyncJobRequest { Instances = ["real-instance"] });

        await WaitForTerminalStatus(client, created.Id);

        var response = await client.GetAsync(
            new Uri($"/api/v1/sync/jobs/{created.Id}/results", UriKind.Relative)
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var results = await response.Content.ReadFromJsonAsync<GetSyncJobResultsResponse>();
        results.Should().NotBeNull();
        results!.Id.Should().Be(created.Id);
        return results;
    }

    // The sync runs on a background task, so results only exist once the job terminates. This is
    // the same poll a real client performs before fetching the results sub-resource.
    private static async Task WaitForTerminalStatus(HttpClient client, Guid jobId)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        var uri = new Uri($"/api/v1/sync/jobs/{jobId}", UriKind.Relative);

        while (DateTimeOffset.UtcNow < deadline)
        {
            var response = await client.GetAsync(uri);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return;
            }

            await Task.Delay(25);
        }

        Assert.Fail($"Sync job {jobId} did not reach a terminal status");
    }

    private static SyncInstanceResult BuildFullResult()
    {
        var transactions = new CustomFormatTransactionData();
        transactions.NewCustomFormats.Add(
            new CustomFormatResource { Name = "New CF", TrashId = "trash1" }
        );
        transactions.UpdatedCustomFormats.Add(
            new CustomFormatResource { Name = "Updated CF", TrashId = "trash2" }
        );
        transactions.DeletedCustomFormats.Add(new TrashIdMapping("trash3", "Deleted CF", 10));
        transactions.UnchangedCustomFormats.Add(
            new CustomFormatResource { Name = "Same CF", TrashId = "trash4" }
        );

        return new SyncInstanceResult
        {
            CustomFormats = new CustomFormatSyncResult(
                transactions,
                new Dictionary<string, CustomFormatSourceInfo>
                {
                    ["trash1"] = new(
                        CfSource.CfGroupImplicit,
                        "My Group",
                        CfInclusionReason.Required,
                        ["profile-a"]
                    ),
                    ["trash2"] = new(CfSource.FlatConfig, null, CfInclusionReason.None, []),
                }
            ),
            QualityProfiles = new QualityProfileSyncResult(BuildProfileTransactions()),
            QualitySizes = new QualitySizeSyncResult(
                [
                    NewUpdatedQualityItem("Bluray-1080p", 5, 100, 95, isDifferent: true),
                    NewUpdatedQualityItem("Bluray-720p", 4, 90, 85, isDifferent: false),
                ],
                new QualityItemLimits(400, 395),
                "movie"
            ),
            SonarrNaming = new SonarrNamingSyncResult(
                new SonarrNamingData { SeriesFolderFormat = "current-series" },
                new SonarrNamingData
                {
                    RenameEpisodes = true,
                    SeriesFolderFormat = "desired-series",
                    SeasonFolderFormat = "desired-season",
                    StandardEpisodeFormat = "desired-standard",
                    DailyEpisodeFormat = "desired-daily",
                    AnimeEpisodeFormat = "desired-anime",
                }
            ),
            RadarrNaming = new RadarrNamingSyncResult(
                new RadarrNamingData { MovieFolderFormat = "current-folder" },
                new RadarrNamingData
                {
                    RenameMovies = true,
                    StandardMovieFormat = "desired-movie",
                    MovieFolderFormat = "desired-folder",
                }
            ),
            MediaManagement = new MediaManagementSyncResult(
                new MediaManagementData { PropersAndRepacks = PropersAndRepacksMode.DoNotPrefer },
                new MediaManagementData { PropersAndRepacks = PropersAndRepacksMode.DoNotUpgrade }
            ),
        };
    }

    private static QualityProfileTransactionData BuildProfileTransactions()
    {
        var transactions = new QualityProfileTransactionData();
        transactions.UpdatedProfiles.Add(
            new ProfileWithStats
            {
                Profile = new UpdatedQualityProfile
                {
                    Profile = new QualityProfileData
                    {
                        Id = 1,
                        Name = "HD Bluray",
                        MinFormatScore = 50,
                    },
                    ProfileConfig = new PlannedQualityProfile.UserDefined
                    {
                        Name = "HD Bluray",
                        Config = new QualityProfileConfig
                        {
                            Name = "HD Bluray",
                            MinFormatScore = 75,
                        },
                    },
                    UpdatedScores =
                    [
                        NewUpdatedScore("Moved CF", 100, 200, FormatScoreUpdateReason.Updated),
                        NewUpdatedScore("Same CF", 300, 300, FormatScoreUpdateReason.Updated),
                        NewUpdatedScore("Untouched CF", 50, 60, FormatScoreUpdateReason.NoChange),
                    ],
                },
            }
        );

        return transactions;
    }

    private static UpdatedFormatScore NewUpdatedScore(
        string name,
        int currentScore,
        int newScore,
        FormatScoreUpdateReason reason
    )
    {
        return new UpdatedFormatScore(
            new QualityProfileFormatItem
            {
                FormatId = 0,
                Name = name,
                Score = currentScore,
            },
            newScore,
            reason
        );
    }

    private static UpdatedQualityItem NewUpdatedQualityItem(
        string quality,
        decimal min,
        decimal max,
        decimal preferred,
        bool isDifferent
    )
    {
        return new UpdatedQualityItem
        {
            Quality = quality,
            Min = min,
            Max = max,
            Preferred = preferred,
            IsDifferent = isDifferent,
            ServerItem = new QualityDefinitionItem { QualityName = quality },
        };
    }
}
