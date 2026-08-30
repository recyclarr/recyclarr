using System.Diagnostics.CodeAnalysis;
using System.Net;
using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Pipelines;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Pipelines.QualityProfile.State;
using Recyclarr.Servarr.QualityProfile;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using Recyclarr.SyncState;
using Refit;

namespace Recyclarr.Core.Tests.Pipelines.QualityProfile;

internal sealed class QualityProfileSyncOperationTest
{
    [Test]
    public async Task Compute_returns_guide_backed_create_delta()
    {
        var resource = NewPlan.QpResource("qp-trash-id", "WEB-1080p") with
        {
            UpgradeAllowed = true,
        };
        var config = new QualityProfileConfig
        {
            Name = "WEB-1080p",
            Qualities = [NewQp.QualityConfig("Bluray-1080p")],
        };
        var planned = NewPlan.Qp(config, resource);
        var harness = CreateHarness(
            [planned],
            [],
            Schema(NewQp.QualityItem(1, "Bluray-1080p", false))
        );

        var compute = await Compute(harness);

        compute.Result.Status.Should().Be(SyncResultStatus.Succeeded);
        var delta = compute
            .Result.Deltas.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<QualityProfileCreateDelta>()
            .Which;
        delta
            .Identity.Should()
            .Be(new GuideBackedQualityProfileIdentity(new MappingKey("qp-trash-id", "WEB-1080p")));
        delta.State.Name.Should().Be("WEB-1080p");
        delta.State.Qualities.Should().Equal(new QualityProfileQuality("Bluray-1080p", true));
    }

    [Test]
    public async Task Compute_returns_explicit_update_components()
    {
        var current = new QualityProfileData
        {
            Id = 7,
            Name = "Old Name",
            UpgradeAllowed = false,
            Cutoff = 1,
            Items = [NewQp.QualityItem(1, "Bluray-1080p", false)],
            FormatItems =
            [
                new QualityProfileFormatItem
                {
                    FormatId = 10,
                    Name = "HDR",
                    Score = 0,
                },
            ],
        };
        var resource = NewPlan.QpResource("qp-trash-id", "New Name");
        var config = new QualityProfileConfig
        {
            Name = "New Name",
            UpgradeAllowed = true,
            UpgradeUntilQuality = "Bluray-1080p",
            Qualities = [NewQp.QualityConfig("Bluray-1080p")],
        };
        var planned = NewPlan.Qp(
            config,
            resource,
            new PlannedCfScore(new PlannedCustomFormat(NewCf.Data("HDR", "cf-trash-id", 10)), 100)
        );
        var mapping = new TrashIdMapping("qp-trash-id", "Old Name", 7);
        var harness = CreateHarness([planned], [current], Schema(current.Items), [mapping]);

        var compute = await Compute(harness);

        var components = compute
            .Result.Deltas.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<QualityProfileUpdateDelta>()
            .Which.Components;
        components.Should().ContainSingle(x => x is QualityProfileNameChanged);
        components.Should().ContainSingle(x => x is QualityProfileUpgradeAllowedChanged);
        components.Should().ContainSingle(x => x is QualityProfileQualityLayoutChanged);
        components.Should().ContainSingle(x => x is QualityProfileCustomFormatScoreChanged);
    }

    [Test]
    public async Task Valid_and_ambiguous_profiles_produce_partial_result()
    {
        var valid = NewPlan.Qp("Valid");
        var ambiguous = NewPlan.Qp("Duplicate");
        var harness = CreateHarness(
            [valid, ambiguous],
            [
                new QualityProfileData { Id = 1, Name = "Valid" },
                new QualityProfileData { Id = 2, Name = "Duplicate" },
                new QualityProfileData { Id = 3, Name = "Duplicate" },
            ],
            Schema()
        );

        var compute = await Compute(harness);

        compute.Result.Status.Should().Be(SyncResultStatus.Partial);
        var outcome = compute
            .Result.Outcomes.OfType<QualityProfileAmbiguousMatchOutcome>()
            .Should()
            .ContainSingle()
            .Which;
        outcome.Identity.Should().Be(new UserDefinedQualityProfileIdentity("Duplicate"));
        outcome.ServiceMatches.Should().HaveCount(2);
    }

    [Test]
    public async Task Validation_failure_is_typed_without_diagnostic_prose()
    {
        var config = new QualityProfileConfig
        {
            Name = "Invalid",
            MinFormatScore = 10,
            Qualities = [NewQp.QualityConfig("Bluray-1080p")],
        };
        var harness = CreateHarness(
            [NewPlan.Qp(config)],
            [],
            Schema(NewQp.QualityItem(1, "Bluray-1080p", false))
        );

        var compute = await Compute(harness);

        compute.Result.Status.Should().Be(SyncResultStatus.Failed);
        compute.Result.Deltas.Should().BeEmpty();
        compute
            .Result.Outcomes.OfType<QualityProfileMinimumScoreUnsatisfiedOutcome>()
            .Should()
            .Equal(
                new QualityProfileMinimumScoreUnsatisfiedOutcome(
                    new UserDefinedQualityProfileIdentity("Invalid"),
                    10,
                    0,
                    0
                )
            );
    }

    [Test]
    public async Task Plan_outcomes_retain_quality_profile_context()
    {
        var harness = CreateHarness([], [], Schema());
        harness.Plan.Add(new InvalidQualityProfileTrashIdOutcome("missing-qp"));
        harness.Plan.Add(new DuplicateQualityProfileNameOutcome("Duplicate"));
        harness.Plan.Add(
            new CustomFormatServiceIdCollisionOutcome(
                "Existing CF",
                "existing-id",
                "Rejected CF",
                "rejected-id",
                10
            )
        );

        var compute = await Compute(harness);

        compute.Result.Status.Should().Be(SyncResultStatus.Failed);
        compute.Result.Outcomes.Should().Contain(x => x is QualityProfileReferenceMismatchOutcome);
        compute.Result.Outcomes.Should().Contain(x => x is QualityProfileDuplicateNameOutcome);
        compute.Result.Outcomes.Should().Contain(x => x is QualityProfileScoreCollisionOutcome);
    }

    [Test]
    public async Task Resource_local_plan_errors_flow_through_normal_orchestration()
    {
        var harness = CreateHarness([], [], Schema());
        harness.Plan.Add(new DuplicateQualityProfileNameOutcome("Duplicate"));
        harness.Plan.Add(
            new CustomFormatServiceIdCollisionOutcome(
                "Existing CF",
                "existing-id",
                "Rejected CF",
                "rejected-id",
                10
            )
        );
        var storage = new InMemorySyncRunStorage();
        var executor = new CompositeSyncPipeline(Substitute.For<ILogger>(), [harness.Sut], storage);
        var instancePublisher = Substitute.For<IInstancePublisher>();
        instancePublisher.ForPipeline(default).ReturnsForAnyArgs(harness.Publisher);
        var settings = Substitute.For<ISyncSettings>();
        settings.Preview.Returns(true);

        harness.Plan.HasInstanceBlockingErrors.Should().BeFalse();

        await executor.Execute(
            settings,
            harness.Plan,
            instancePublisher,
            "instance",
            CancellationToken.None
        );

        var result = storage.Retrieve<QualityProfileComputeResult>(
            "instance",
            PipelineType.QualityProfile
        );
        result.Should().NotBeNull();
        result.Result.Outcomes.Should().Contain(x => x is QualityProfileDuplicateNameOutcome);
        result.Result.Outcomes.Should().Contain(x => x is QualityProfileScoreCollisionOutcome);
    }

    [Test]
    public async Task Missing_and_adopted_profiles_retain_identity()
    {
        var missing = NewPlan.Qp("Missing", shouldCreate: false);
        var adopted = GuideProfile("Adopted", "adopted-id");
        var harness = CreateHarness(
            [missing, adopted],
            [new QualityProfileData { Id = 7, Name = "Adopted" }],
            Schema()
        );

        var compute = await Compute(harness);

        compute.Result.Status.Should().Be(SyncResultStatus.Partial);
        compute
            .Result.Outcomes.OfType<QualityProfileNotFoundOutcome>()
            .Should()
            .Equal(
                new QualityProfileNotFoundOutcome(new UserDefinedQualityProfileIdentity("Missing"))
            );
        compute
            .Result.Outcomes.OfType<QualityProfileAdoptedOutcome>()
            .Should()
            .Equal(
                new QualityProfileAdoptedOutcome(
                    new GuideBackedQualityProfileIdentity(new MappingKey("adopted-id", "Adopted")),
                    7
                )
            );
    }

    [Test]
    public async Task Rename_conflict_retains_the_conflicting_service_profile()
    {
        var planned = GuideProfile("New Name", "trash-id");
        var harness = CreateHarness(
            [planned],
            [
                new QualityProfileData { Id = 7, Name = "Old Name" },
                new QualityProfileData { Id = 8, Name = "New Name" },
            ],
            Schema(),
            [new TrashIdMapping("trash-id", "Old Name", 7)]
        );

        var compute = await Compute(harness);

        compute.Result.Status.Should().Be(SyncResultStatus.Failed);
        compute
            .Result.Outcomes.OfType<QualityProfileRenameBlockedOutcome>()
            .Should()
            .Equal(
                new QualityProfileRenameBlockedOutcome(
                    new GuideBackedQualityProfileIdentity(new MappingKey("trash-id", "New Name")),
                    new QualityProfileServiceMatch("New Name", 8)
                )
            );
    }

    [Test]
    public async Task Invalid_cutoff_reference_is_a_typed_constraint()
    {
        var config = new QualityProfileConfig
        {
            Name = "Invalid",
            UpgradeUntilQuality = "Unknown",
            Qualities = [NewQp.QualityConfig("Unknown")],
        };
        var harness = CreateHarness(
            [NewPlan.Qp(config)],
            [],
            Schema(NewQp.QualityItem(1, "Bluray-1080p", false))
        );

        var compute = await Compute(harness);

        compute
            .Result.Outcomes.OfType<QualityProfileInvalidCutoffOutcome>()
            .Should()
            .Equal(
                new QualityProfileInvalidCutoffOutcome(
                    new UserDefinedQualityProfileIdentity("Invalid"),
                    "Unknown"
                )
            );
    }

    [Test]
    public async Task Unavailable_cutoff_is_a_typed_constraint()
    {
        var config = new QualityProfileConfig
        {
            Name = "Existing",
            UpgradeUntilQuality = "Bluray-1080p",
        };
        var serviceProfile = new QualityProfileData
        {
            Id = 7,
            Name = "Existing",
            Items = [NewQp.QualityItem(1, "Bluray-1080p", false)],
        };
        var harness = CreateHarness([NewPlan.Qp(config)], [serviceProfile], Schema());

        var compute = await Compute(harness);

        compute
            .Result.Outcomes.OfType<QualityProfileUnavailableCutoffOutcome>()
            .Should()
            .Equal(
                new QualityProfileUnavailableCutoffOutcome(
                    new UserDefinedQualityProfileIdentity("Existing"),
                    "Bluray-1080p"
                )
            );
    }

    [Test]
    public async Task Missing_create_qualities_is_a_typed_constraint()
    {
        var harness = CreateHarness([NewPlan.Qp("New")], [], Schema());

        var compute = await Compute(harness);

        compute
            .Result.Outcomes.OfType<QualityProfileQualitiesRequiredOutcome>()
            .Should()
            .Equal(
                new QualityProfileQualitiesRequiredOutcome(
                    new UserDefinedQualityProfileIdentity("New")
                )
            );
    }

    [Test]
    public async Task Non_blocking_reference_mismatches_preserve_success()
    {
        var config = new QualityProfileConfig
        {
            Name = "Existing",
            Qualities = [NewQp.QualityConfig("Unknown Quality")],
            ResetUnmatchedScores = new ResetUnmatchedScoresConfig
            {
                Except = ["Unknown CF"],
                ExceptPatterns = ["missing.*"],
            },
        };
        var serviceProfile = new QualityProfileData
        {
            Id = 7,
            Name = "Existing",
            FormatItems = [new QualityProfileFormatItem { FormatId = 10, Name = "Known CF" }],
        };
        var harness = CreateHarness([NewPlan.Qp(config)], [serviceProfile], Schema());

        var compute = await Compute(harness);

        compute.Result.Status.Should().Be(SyncResultStatus.Succeeded);
        compute
            .Result.Outcomes.OfType<QualityProfileQualityReferenceMismatchOutcome>()
            .Should()
            .ContainSingle();
        compute
            .Result.Outcomes.OfType<QualityProfileResetScoreReferenceMismatchOutcome>()
            .Should()
            .ContainSingle();
    }

    [Test]
    public async Task Reset_score_produces_an_explicit_score_delta()
    {
        var config = new QualityProfileConfig
        {
            Name = "Existing",
            ResetUnmatchedScores = new ResetUnmatchedScoresConfig { Enabled = true },
        };
        var serviceProfile = new QualityProfileData
        {
            Id = 7,
            Name = "Existing",
            FormatItems =
            [
                new QualityProfileFormatItem
                {
                    FormatId = 10,
                    Name = "HDR",
                    Score = 100,
                },
            ],
        };
        var harness = CreateHarness([NewPlan.Qp(config)], [serviceProfile], Schema());

        var compute = await Compute(harness);

        var component = compute
            .Result.Deltas.OfType<QualityProfileUpdateDelta>()
            .Single()
            .Components.OfType<QualityProfileCustomFormatScoreChanged>()
            .Single();
        component.Value.Should().Be(new ValueDelta<int>(100, 0));
        component.Reason.Should().Be(QualityProfileScoreChangeReason.Reset);
    }

    [Test]
    public async Task Missing_service_quality_produces_a_concrete_layout_delta()
    {
        var serviceProfile = new QualityProfileData
        {
            Id = 7,
            Name = "Existing",
            Items = [NewQp.QualityItem(1, "Bluray-1080p", true)],
        };
        var harness = CreateHarness(
            [NewPlan.Qp("Existing")],
            [serviceProfile],
            Schema(
                NewQp.QualityItem(1, "Bluray-1080p", true),
                NewQp.QualityItem(2, "WEB 1080p", false)
            )
        );

        var compute = await Compute(harness);

        var layout = compute
            .Result.Deltas.OfType<QualityProfileUpdateDelta>()
            .Single()
            .Components.OfType<QualityProfileQualityLayoutChanged>()
            .Single();
        layout.Current.Should().ContainSingle();
        layout.Desired.Should().HaveCount(2);
    }

    [TestCase(HttpStatusCode.BadRequest)]
    [TestCase(HttpStatusCode.NotFound)]
    [TestCase(HttpStatusCode.Conflict)]
    [TestCase(HttpStatusCode.UnprocessableEntity)]
    public async Task Apply_continues_after_create_rejection_and_saves_acknowledged_state(
        HttpStatusCode statusCode
    )
    {
        var rejected = GuideProfile("Rejected", "rejected-id");
        var accepted = GuideProfile("Accepted", "accepted-id");
        var harness = CreateHarness(
            [rejected, accepted],
            [],
            Schema(NewQp.QualityItem(1, "Bluray-1080p", false))
        );
        var exception = await CreateApiException(statusCode);
        harness
            .Service.CreateQualityProfile(default!, default)
            .ReturnsForAnyArgs(call =>
            {
                var profile = call.Arg<QualityProfileData>();
                return profile.Name == "Rejected"
                    ? Task.FromException<QualityProfileData>(exception)
                    : Task.FromResult(profile with { Id = 9 });
            });

        var compute = await Compute(harness);
        var calculatedDeltas = compute.Result.Deltas;
        await Persist(harness, compute);

        compute.Result.Status.Should().Be(SyncResultStatus.Partial);
        compute.Result.Deltas.Should().Equal(calculatedDeltas);
        compute
            .Result.Outcomes.OfType<QualityProfileCreateRejectedOutcome>()
            .Should()
            .ContainSingle()
            .Which.Identity.Should()
            .Be(new GuideBackedQualityProfileIdentity(new MappingKey("rejected-id", "Rejected")));
        harness.State.Mappings.Should().Equal(new TrashIdMapping("accepted-id", "Accepted", 9));
    }

    [Test]
    public async Task Update_rejection_retains_old_mapping_while_success_records_rename()
    {
        var rejected = GuideProfile("Rejected New", "rejected-id");
        var accepted = GuideProfile("Accepted New", "accepted-id");
        var oldRejected = new QualityProfileData { Id = 7, Name = "Rejected Old" };
        var oldAccepted = new QualityProfileData { Id = 8, Name = "Accepted Old" };
        var rejectedMapping = new TrashIdMapping("rejected-id", "Rejected Old", 7);
        var acceptedMapping = new TrashIdMapping("accepted-id", "Accepted Old", 8);
        var harness = CreateHarness(
            [rejected, accepted],
            [oldRejected, oldAccepted],
            Schema(),
            [rejectedMapping, acceptedMapping]
        );
        var exception = await CreateApiException(HttpStatusCode.BadRequest);
        harness
            .Service.UpdateQualityProfile(default!, default)
            .ReturnsForAnyArgs(call =>
                call.Arg<QualityProfileData>().Name == "Rejected New"
                    ? Task.FromException(exception)
                    : Task.CompletedTask
            );

        var compute = await Compute(harness);
        await Persist(harness, compute);

        compute.Result.Status.Should().Be(SyncResultStatus.Partial);
        compute
            .Result.Outcomes.OfType<QualityProfileUpdateRejectedOutcome>()
            .Should()
            .ContainSingle();
        harness
            .State.Mappings.Should()
            .Equal(rejectedMapping, new TrashIdMapping("accepted-id", "Accepted New", 8));
    }

    [Test]
    public async Task Instance_wide_api_failure_propagates_without_saving_state()
    {
        var profile = GuideProfile("Unauthorized", "trash-id");
        var harness = CreateHarness(
            [profile],
            [],
            Schema(NewQp.QualityItem(1, "Bluray-1080p", false))
        );
        var exception = await CreateApiException(HttpStatusCode.Unauthorized);
        harness
            .Service.CreateQualityProfile(default!, default)
            .ReturnsForAnyArgs(Task.FromException<QualityProfileData>(exception));

        var compute = await Compute(harness);
        var act = () => Persist(harness, compute);

        await act.Should().ThrowAsync<ApiException>();
        harness.StatePersister.DidNotReceive().Save(harness.State);
    }

    [Test]
    public async Task Sync_state_persistence_failure_propagates()
    {
        var profile = GuideProfile("Accepted", "trash-id");
        var harness = CreateHarness(
            [profile],
            [],
            Schema(NewQp.QualityItem(1, "Bluray-1080p", false))
        );
        harness
            .Service.CreateQualityProfile(default!, default)
            .ReturnsForAnyArgs(call => call.Arg<QualityProfileData>() with { Id = 9 });
        harness
            .StatePersister.When(x => x.Save(harness.State))
            .Do(_ => throw new IOException("state write failed"));

        var compute = await Compute(harness);
        var act = () => Persist(harness, compute);

        await act.Should().ThrowAsync<IOException>().WithMessage("state write failed");
    }

    [Test]
    public async Task Preview_returns_deltas_without_service_or_state_writes()
    {
        var profile = GuideProfile("Preview", "trash-id");
        var harness = CreateHarness(
            [profile],
            [],
            Schema(NewQp.QualityItem(1, "Bluray-1080p", false))
        );
        var storage = new InMemorySyncRunStorage();
        var executor = new CompositeSyncPipeline(Substitute.For<ILogger>(), [harness.Sut], storage);
        var instancePublisher = Substitute.For<IInstancePublisher>();
        instancePublisher.ForPipeline(default).ReturnsForAnyArgs(harness.Publisher);
        var settings = Substitute.For<ISyncSettings>();
        settings.Preview.Returns(true);

        await executor.Execute(
            settings,
            harness.Plan,
            instancePublisher,
            "instance",
            CancellationToken.None
        );

        var result = storage.Retrieve<QualityProfileComputeResult>(
            "instance",
            PipelineType.QualityProfile
        );
        result.Should().NotBeNull();
        result.Result.Deltas.Should().ContainSingle();
        await harness.Service.DidNotReceiveWithAnyArgs().CreateQualityProfile(default!, default);
        harness.StatePersister.DidNotReceive().Save(harness.State);
    }

    private static PlannedQualityProfile GuideProfile(string name, string trashId)
    {
        return NewPlan.Qp(
            new QualityProfileConfig
            {
                Name = name,
                Qualities = [NewQp.QualityConfig("Bluray-1080p")],
            },
            NewPlan.QpResource(trashId, name)
        );
    }

    private static QualityProfileData Schema(params QualityProfileItem[] items)
    {
        return Schema((IReadOnlyList<QualityProfileItem>)items);
    }

    private static QualityProfileData Schema(IReadOnlyList<QualityProfileItem> items)
    {
        return new QualityProfileData { Name = "Schema", Items = items };
    }

    private static Harness CreateHarness(
        IReadOnlyList<PlannedQualityProfile> planned,
        IReadOnlyList<QualityProfileData> serviceProfiles,
        QualityProfileData schema,
        IReadOnlyList<TrashIdMapping>? mappings = null
    )
    {
        var service = Substitute.For<IQualityProfileService>();
        service.GetQualityProfiles(default).ReturnsForAnyArgs(serviceProfiles);
        service.GetSchema(default).ReturnsForAnyArgs(schema);
        service.GetLanguages(default).ReturnsForAnyArgs([]);

        var state = new TrashIdMappingStore(mappings?.ToList() ?? []);
        var statePersister = Substitute.For<IQualityProfileStatePersister>();
        statePersister.Load().Returns(state);

        var plan = new TestPlan();
        foreach (var profile in planned)
        {
            plan.AddQualityProfile(profile);
        }

        var log = Substitute.For<ILogger>();
        var sut = new QualityProfileSyncOperation(
            log,
            service,
            statePersister,
            new QualityProfileStatCalculator(log),
            new QualityProfileLogger(log)
        );
        return new Harness(
            sut,
            service,
            statePersister,
            state,
            plan,
            Substitute.For<IPipelinePublisher>()
        );
    }

    private static async Task<QualityProfileComputeResult> Compute(Harness harness)
    {
        var result = await ((ISyncOperation)harness.Sut).Compute(
            harness.Plan,
            harness.Publisher,
            CancellationToken.None
        );
        return result.Should().BeOfType<QualityProfileComputeResult>().Which;
    }

    private static async Task Persist(Harness harness, QualityProfileComputeResult computeResult)
    {
        await ((ISyncOperation)harness.Sut).Persist(
            computeResult,
            harness.Publisher,
            CancellationToken.None
        );
    }

    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "ApiException.Create takes ownership of request and response"
    )]
    private static Task<ApiException> CreateApiException(HttpStatusCode statusCode)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "http://localhost/api/v3/qualityprofile"
        );
        var response = new HttpResponseMessage(statusCode);
        return ApiException.Create(request, HttpMethod.Post, response, new RefitSettings());
    }

    private sealed record Harness(
        QualityProfileSyncOperation Sut,
        IQualityProfileService Service,
        IQualityProfileStatePersister StatePersister,
        TrashIdMappingStore State,
        TestPlan Plan,
        IPipelinePublisher Publisher
    );
}
