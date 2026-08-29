using System.Diagnostics.CodeAnalysis;
using System.Net;
using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Pipelines;
using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Pipelines.CustomFormat.State;
using Recyclarr.Pipelines.Plan;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Servarr.CustomFormat;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using Recyclarr.SyncState;
using Recyclarr.TrashGuide.CustomFormat;
using Refit;

namespace Recyclarr.Core.Tests.Pipelines.CustomFormat;

internal sealed class CustomFormatSyncOperationTest
{
    [Test]
    public async Task Compute_returns_create_delta_with_selection_provenance()
    {
        var cf = NewCf.Data("Release Title", "trash-id");
        var planned = new PlannedCustomFormat(cf)
        {
            Source = CfSource.CfGroupExplicit,
            GroupName = "Streaming Services",
            InclusionReason = CfInclusionReason.Required,
            AssignScoresTo = [new AssignScoresToConfig { Name = "WEB-1080p" }],
        };
        var harness = CreateHarness([planned], []);

        var compute = await Compute(harness);

        compute.Result.Status.Should().Be(SyncResultStatus.Succeeded);
        var delta = compute.Result.Deltas.Should().ContainSingle().Which;
        delta.Should().BeOfType<CustomFormatCreateDelta>();
        delta
            .Should()
            .BeEquivalentTo(
                new CustomFormatCreateDelta(
                    new CustomFormatIdentity("trash-id", "Release Title"),
                    new CustomFormatSourceInfo(
                        CfSource.CfGroupExplicit,
                        "Streaming Services",
                        CfInclusionReason.Required,
                        ["WEB-1080p"]
                    )
                )
            );
    }

    [Test]
    public async Task Compute_returns_explicit_update_components()
    {
        var desired = NewCf.Data("New Name", "trash-id", 7) with
        {
            IncludeCustomFormatWhenRenaming = true,
            Specifications =
            [
                Specification("Release Title", "new"),
                Specification("Added", "value"),
            ],
        };
        var current = NewCf.Data("Old Name", "", 7) with
        {
            Specifications =
            [
                Specification("Release Title", "old"),
                Specification("Removed", "value"),
            ],
        };
        var harness = CreateHarness(
            [new PlannedCustomFormat(desired)],
            [current],
            [new TrashIdMapping("trash-id", "New Name", 7)]
        );

        var compute = await Compute(harness);

        var delta = compute
            .Result.Deltas.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<CustomFormatUpdateDelta>()
            .Which;
        CustomFormatUpdateComponent[] expected =
        [
            new CustomFormatNameChanged(new ValueDelta<string>("Old Name", "New Name")),
            new CustomFormatIncludeWhenRenamingChanged(new ValueDelta<bool>(false, true)),
            new CustomFormatSpecificationChanged("Release Title"),
            new CustomFormatSpecificationAdded("Added"),
            new CustomFormatSpecificationRemoved("Removed"),
        ];
        delta.Components.Should().Equal(expected);
    }

    [Test]
    public async Task Same_named_service_resource_produces_adoption_outcome()
    {
        var desired = NewCf.Data("Existing", "trash-id");
        var harness = CreateHarness(
            [new PlannedCustomFormat(desired)],
            [NewCf.Data("Existing", "", 7)]
        );

        var compute = await Compute(harness);

        compute.Result.Status.Should().Be(SyncResultStatus.Succeeded);
        compute.Result.Deltas.Should().BeEmpty();
        compute
            .Result.Outcomes.OfType<CustomFormatAdoptedOutcome>()
            .Should()
            .ContainSingle()
            .Which.Should()
            .Be(
                new CustomFormatAdoptedOutcome(new CustomFormatIdentity("trash-id", "Existing"), 7)
            );
    }

    [Test]
    public async Task Valid_and_ambiguous_resources_produce_partial_result()
    {
        var valid = new PlannedCustomFormat(NewCf.Data("Valid", "valid-id"));
        var ambiguous = new PlannedCustomFormat(NewCf.Data("Duplicate", "duplicate-id"));
        var harness = CreateHarness(
            [valid, ambiguous],
            [NewCf.Data("Duplicate", "", 1), NewCf.Data("Duplicate", "", 2)]
        );

        var compute = await Compute(harness);

        compute.Result.Status.Should().Be(SyncResultStatus.Partial);
        compute.Result.Deltas.OfType<CustomFormatCreateDelta>().Should().ContainSingle();
        var outcome = compute
            .Result.Outcomes.OfType<CustomFormatAmbiguousMatchOutcome>()
            .Should()
            .ContainSingle()
            .Which;
        outcome.Identity.Should().Be(new CustomFormatIdentity("duplicate-id", "Duplicate"));
        outcome.ServiceMatches.Should().HaveCount(2);
    }

    [Test]
    public async Task Reference_mismatch_without_completed_resources_fails()
    {
        var harness = CreateHarness([], []);
        harness.Plan.Add(new InvalidCustomFormatTrashIdOutcome("missing-id"));

        var compute = await Compute(harness);

        compute.Result.Status.Should().Be(SyncResultStatus.Failed);
        compute
            .Result.Outcomes.OfType<CustomFormatReferenceMismatchOutcome>()
            .Should()
            .ContainSingle()
            .Which.TrashId.Should()
            .Be("missing-id");
    }

    [Test]
    public async Task Disabled_deletion_produces_no_delta()
    {
        var serviceCf = NewCf.Data("Orphan", "", 7);
        var harness = CreateHarness(
            [],
            [serviceCf],
            [new TrashIdMapping("orphan-id", "Orphan", 7)],
            deleteOldCustomFormats: false
        );

        var compute = await Compute(harness);

        compute.Result.Status.Should().Be(SyncResultStatus.Succeeded);
        compute.Result.Deltas.Should().BeEmpty();
        compute.Transactions.DeletedCustomFormats.Should().BeEmpty();
    }

    [Test]
    public async Task Enabled_deletion_produces_concrete_delete_delta()
    {
        var serviceCf = NewCf.Data("Orphan", "", 7);
        var harness = CreateHarness(
            [],
            [serviceCf],
            [new TrashIdMapping("orphan-id", "Orphan", 7)],
            deleteOldCustomFormats: true
        );

        var compute = await Compute(harness);

        compute.Result.Status.Should().Be(SyncResultStatus.Succeeded);
        compute
            .Result.Deltas.Should()
            .Equal(new CustomFormatDeleteDelta(new CustomFormatIdentity("orphan-id", "Orphan")));
    }

    [Test]
    public async Task Conflicting_state_mapping_is_a_typed_partial_outcome()
    {
        var desired = NewCf.Data("Managed", "managed-id", 7);
        var harness = CreateHarness(
            [new PlannedCustomFormat(desired)],
            [NewCf.Data("Managed", "", 7)],
            [
                new TrashIdMapping("managed-id", "Managed", 7),
                new TrashIdMapping("orphan-id", "Orphan", 7),
            ],
            deleteOldCustomFormats: true
        );

        var compute = await Compute(harness);

        compute.Result.Status.Should().Be(SyncResultStatus.Partial);
        var outcome = compute
            .Result.Outcomes.OfType<CustomFormatStateConflictOutcome>()
            .Should()
            .ContainSingle()
            .Which;
        outcome.Identity.Should().Be(new CustomFormatIdentity("orphan-id", "Orphan"));
        outcome.ManagedIdentity.Should().Be(new CustomFormatIdentity("managed-id", "Managed"));
        compute.Result.Deltas.Should().BeEmpty();
    }

    [Test]
    public async Task Apply_continues_after_rejection_and_saves_acknowledged_state()
    {
        var rejected = NewCf.Data("Rejected", "rejected-id");
        var accepted = NewCf.Data("Accepted", "accepted-id");
        var harness = CreateHarness(
            [new PlannedCustomFormat(rejected), new PlannedCustomFormat(accepted)],
            []
        );
        var exception = await CreateApiException(HttpStatusCode.BadRequest);
        harness
            .Api.CreateCustomFormat(rejected, default)
            .Returns(Task.FromException<CustomFormatResource?>(exception));
        harness
            .Api.CreateCustomFormat(accepted, default)
            .Returns(NewCf.Data("Accepted", "accepted-id", 9));

        var compute = await Compute(harness);
        var calculatedDeltas = compute.Result.Deltas;
        await Persist(harness, compute);

        compute.Result.Status.Should().Be(SyncResultStatus.Partial);
        compute.Result.Deltas.Should().Equal(calculatedDeltas);
        compute
            .Result.Outcomes.OfType<CustomFormatCreateRejectedOutcome>()
            .Should()
            .ContainSingle()
            .Which.Identity.Should()
            .Be(new CustomFormatIdentity("rejected-id", "Rejected"));
        harness.State.Mappings.Should().Equal(new TrashIdMapping("accepted-id", "Accepted", 9));
    }

    [Test]
    public async Task Update_and_delete_rejections_retain_deltas_and_existing_state()
    {
        var desired = NewCf.Data("Managed", "managed-id", 7) with
        {
            IncludeCustomFormatWhenRenaming = true,
        };
        var managedMapping = new TrashIdMapping("managed-id", "Managed", 7);
        var orphanMapping = new TrashIdMapping("orphan-id", "Orphan", 8);
        var harness = CreateHarness(
            [new PlannedCustomFormat(desired)],
            [NewCf.Data("Managed", "", 7), NewCf.Data("Orphan", "", 8)],
            [managedMapping, orphanMapping],
            deleteOldCustomFormats: true
        );
        var exception = await CreateApiException(HttpStatusCode.BadRequest);
        harness.Api.UpdateCustomFormat(desired, default).Returns(Task.FromException(exception));
        harness.Api.DeleteCustomFormat(8, default).Returns(Task.FromException(exception));

        var compute = await Compute(harness);
        await Persist(harness, compute);

        compute.Result.Status.Should().Be(SyncResultStatus.Failed);
        compute.Result.Deltas.Should().HaveCount(2);
        compute
            .Result.Outcomes.OfType<CustomFormatUpdateRejectedOutcome>()
            .Should()
            .ContainSingle();
        compute
            .Result.Outcomes.OfType<CustomFormatDeleteRejectedOutcome>()
            .Should()
            .ContainSingle();
        harness.State.Mappings.Should().Equal(managedMapping, orphanMapping);
    }

    [Test]
    public async Task Instance_wide_api_failure_propagates_without_saving_state()
    {
        var cf = NewCf.Data("Unauthorized", "trash-id");
        var harness = CreateHarness([new PlannedCustomFormat(cf)], []);
        var exception = await CreateApiException(HttpStatusCode.Unauthorized);
        harness
            .Api.CreateCustomFormat(cf, default)
            .Returns(Task.FromException<CustomFormatResource?>(exception));

        var compute = await Compute(harness);
        var act = () => Persist(harness, compute);

        await act.Should().ThrowAsync<ApiException>();
        harness.StatePersister.DidNotReceive().Save(harness.State);
    }

    [Test]
    public async Task Preview_returns_deltas_without_service_or_state_writes()
    {
        var cf = NewCf.Data("Preview", "trash-id");
        var harness = CreateHarness([new PlannedCustomFormat(cf)], []);
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

        var result = storage.Retrieve<CustomFormatComputeResult>(
            "instance",
            PipelineType.CustomFormat
        );
        result.Should().NotBeNull();
        result.Result.Deltas.Should().ContainSingle();
        await harness.Api.DidNotReceive().CreateCustomFormat(cf, default);
        harness.StatePersister.DidNotReceive().Save(harness.State);
    }

    private static CustomFormatSpecificationData Specification(string name, string value)
    {
        return new CustomFormatSpecificationData
        {
            Name = name,
            Implementation = "ReleaseTitleSpecification",
            Fields = [NewCf.Field("value", value)],
        };
    }

    private static Harness CreateHarness(
        IReadOnlyList<PlannedCustomFormat> planned,
        IReadOnlyList<CustomFormatResource> serviceCfs,
        IReadOnlyList<TrashIdMapping>? mappings = null,
        bool deleteOldCustomFormats = false
    )
    {
        var api = Substitute.For<ICustomFormatService>();
        api.GetCustomFormats(default).ReturnsForAnyArgs(serviceCfs);

        var state = new TrashIdMappingStore(mappings?.ToList() ?? []);
        var statePersister = Substitute.For<ICustomFormatStatePersister>();
        statePersister.Load().Returns(state);

        var config = Substitute.For<IServiceConfiguration>();
        config.DeleteOldCustomFormats.Returns(deleteOldCustomFormats);

        var plan = new TestPlan();
        foreach (var cf in planned)
        {
            plan.AddCustomFormat(cf);
        }

        var publisher = Substitute.For<IPipelinePublisher>();
        var log = Substitute.For<ILogger>();
        var logger = new CustomFormatTransactionLogger(log);
        var sut = new CustomFormatSyncOperation(log, api, statePersister, logger, config);
        return new Harness(sut, api, statePersister, state, plan, publisher);
    }

    private static async Task<CustomFormatComputeResult> Compute(Harness harness)
    {
        var result = await ((ISyncOperation)harness.Sut).Compute(
            harness.Plan,
            harness.Publisher,
            CancellationToken.None
        );
        return result.Should().BeOfType<CustomFormatComputeResult>().Which;
    }

    private static async Task Persist(Harness harness, CustomFormatComputeResult compute)
    {
        await ((ISyncOperation)harness.Sut).Persist(
            compute,
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
            "http://localhost/api/v3/customformat"
        );
        var response = new HttpResponseMessage(statusCode);
        return ApiException.Create(request, HttpMethod.Post, response, new RefitSettings());
    }

    private sealed record Harness(
        CustomFormatSyncOperation Sut,
        ICustomFormatService Api,
        ICustomFormatStatePersister StatePersister,
        TrashIdMappingStore State,
        TestPlan Plan,
        IPipelinePublisher Publisher
    );
}
