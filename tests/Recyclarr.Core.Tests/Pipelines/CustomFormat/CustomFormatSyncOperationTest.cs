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
    public async Task Compute_returns_created_resource_with_selection_provenance()
    {
        var cf = NewCf.Data("Release Title", "trash-id") with
        {
            Specifications = [Specification("Release Title", "value")],
        };
        var planned = new PlannedCustomFormat(cf)
        {
            Source = CfSource.CfGroupExplicit,
            GroupName = "Streaming Services",
            InclusionReason = CfInclusionReason.Required,
            AssignScoresTo = [new AssignScoresToConfig { Name = "WEB-1080p" }],
        };
        var harness = CreateHarness([planned], []);

        var compute = await Compute(harness);

        compute.Status.Should().Be(SyncResultStatus.Succeeded);
        compute
            .Resources.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new PlannedCustomFormatResource
                {
                    Action = PlannedAction.Create,
                    Identity = new CustomFormatIdentity("trash-id", "Release Title"),
                    SelectionProvenance = new CustomFormatSourceInfo(
                        CfSource.CfGroupExplicit,
                        "Streaming Services",
                        CfInclusionReason.Required,
                        ["WEB-1080p"]
                    ),
                    Name = PlannedValue.Created("Release Title"),
                    IncludeWhenRenaming = PlannedValue.Created(false),
                    Specifications =
                    [
                        new CustomFormatSpecificationResult(
                            "Release Title",
                            CustomFormatSpecificationChange.Added
                        ),
                    ],
                }
            );
    }

    [Test]
    public async Task Unchanged_custom_format_is_reported_with_provenance()
    {
        var desired = NewCf.Data("Same", "trash-id", 7) with
        {
            Specifications = [Specification("Release Title", "value")],
        };
        var harness = CreateHarness(
            [new PlannedCustomFormat(desired)],
            [desired with { TrashId = "" }],
            [NewSyncState.Mapping("trash-id", "Same", 7)]
        );

        var compute = await Compute(harness);

        compute
            .Resources.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new PlannedCustomFormatResource
                {
                    Action = PlannedAction.Unchanged,
                    Identity = new CustomFormatIdentity("trash-id", "Same"),
                    SelectionProvenance = new CustomFormatSourceInfo(
                        CfSource.FlatConfig,
                        null,
                        CfInclusionReason.None,
                        []
                    ),
                    Name = PlannedValue.Compared("Same", "Same"),
                    IncludeWhenRenaming = PlannedValue.Compared(false, false),
                    Specifications =
                    [
                        new CustomFormatSpecificationResult(
                            "Release Title",
                            CustomFormatSpecificationChange.Unchanged
                        ),
                    ],
                }
            );
    }

    [Test]
    public async Task Compute_returns_every_value_of_an_updated_resource()
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
            [NewSyncState.Mapping("trash-id", "New Name", 7)]
        );

        var compute = await Compute(harness);

        var resource = compute.Resources.Should().ContainSingle().Which;
        resource.Action.Should().Be(PlannedAction.Update);
        resource.Name.Should().Be(PlannedValue.Compared("Old Name", "New Name"));
        resource.IncludeWhenRenaming.Should().Be(PlannedValue.Compared(false, true));
        resource
            .Specifications.Should()
            .Equal(
                new CustomFormatSpecificationResult(
                    "Release Title",
                    CustomFormatSpecificationChange.Changed
                ),
                new CustomFormatSpecificationResult("Added", CustomFormatSpecificationChange.Added),
                new CustomFormatSpecificationResult(
                    "Removed",
                    CustomFormatSpecificationChange.Removed
                )
            );
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

        compute.Status.Should().Be(SyncResultStatus.Succeeded);
        compute
            .Resources.Should()
            .ContainSingle()
            .Which.Action.Should()
            .Be(PlannedAction.Unchanged);
        compute
            .Outcomes.OfType<CustomFormatAdoptedOutcome>()
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

        compute.Status.Should().Be(SyncResultStatus.Partial);
        compute.Resources.Should().ContainSingle().Which.Action.Should().Be(PlannedAction.Create);
        var outcome = compute
            .Outcomes.OfType<CustomFormatAmbiguousMatchOutcome>()
            .Should()
            .ContainSingle()
            .Which;
        outcome.Identity.Should().Be(new CustomFormatIdentity("duplicate-id", "Duplicate"));
        outcome.ServiceMatches.Should().HaveCount(2);
    }

    [Test]
    public async Task Duplicate_names_without_completed_resources_fail()
    {
        var harness = CreateHarness([], []);
        harness.PlanOutcomes.Add(new DuplicateCustomFormatNameOutcome("Shared", ["cf-a", "cf-b"]));

        var compute = await Compute(harness);

        compute.Status.Should().Be(SyncResultStatus.Failed);
        compute
            .Outcomes.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(new CustomFormatDuplicateNameOutcome("Shared", ["cf-a", "cf-b"]));
    }

    [Test]
    public async Task Synced_custom_format_skipped_for_duplicate_name_is_not_deleted()
    {
        var harness = CreateHarness(
            [],
            [NewCf.Data("Shared", "", 7)],
            [NewSyncState.Mapping("cf-a", "Shared", 7)],
            deleteOldCustomFormats: true
        );
        harness.PlanOutcomes.Add(new DuplicateCustomFormatNameOutcome("Shared", ["cf-a", "cf-b"]));

        var compute = await Compute(harness);

        compute.Resources.Should().BeEmpty();
        compute.Deletes.Should().BeEmpty();
    }

    [Test]
    public async Task Disabled_deletion_produces_no_delete()
    {
        var serviceCf = NewCf.Data("Orphan", "", 7);
        var harness = CreateHarness(
            [],
            [serviceCf],
            [NewSyncState.Mapping("orphan-id", "Orphan", 7)],
            deleteOldCustomFormats: false
        );

        var compute = await Compute(harness);

        compute.Status.Should().Be(SyncResultStatus.Succeeded);
        compute.Deletes.Should().BeEmpty();
    }

    [Test]
    public async Task Enabled_deletion_reports_the_deleted_custom_format()
    {
        var serviceCf = NewCf.Data("Orphan", "", 7);
        var harness = CreateHarness(
            [],
            [serviceCf],
            [NewSyncState.Mapping("orphan-id", "Orphan", 7)],
            deleteOldCustomFormats: true
        );

        var compute = await Compute(harness);

        compute.Status.Should().Be(SyncResultStatus.Succeeded);
        compute.Deletes.Should().Equal(new CustomFormatIdentity("orphan-id", "Orphan"));
    }

    [Test]
    public async Task Conflicting_state_mapping_is_a_typed_partial_outcome()
    {
        var desired = NewCf.Data("Managed", "managed-id", 7);
        var harness = CreateHarness(
            [new PlannedCustomFormat(desired)],
            [NewCf.Data("Managed", "", 7)],
            [
                NewSyncState.Mapping("managed-id", "Managed", 7),
                NewSyncState.Mapping("orphan-id", "Orphan", 7),
            ],
            deleteOldCustomFormats: true
        );

        var compute = await Compute(harness);

        compute.Status.Should().Be(SyncResultStatus.Partial);
        var outcome = compute
            .Outcomes.OfType<CustomFormatStateConflictOutcome>()
            .Should()
            .ContainSingle()
            .Which;
        outcome.Identity.Should().Be(new CustomFormatIdentity("orphan-id", "Orphan"));
        outcome.ManagedIdentity.Should().Be(new CustomFormatIdentity("managed-id", "Managed"));
        compute.Deletes.Should().BeEmpty();
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
        var calculated = compute.Resources;
        compute = await Persist(harness);

        compute.Status.Should().Be(SyncResultStatus.Partial);
        compute.Resources.Should().BeEquivalentTo(calculated);
        compute
            .Outcomes.OfType<CustomFormatCreateRejectedOutcome>()
            .Should()
            .ContainSingle()
            .Which.Identity.Should()
            .Be(new CustomFormatIdentity("rejected-id", "Rejected"));
        harness.State.Mappings.Should().Equal(NewSyncState.Mapping("accepted-id", "Accepted", 9));
    }

    [Test]
    public async Task Update_and_delete_rejections_retain_results_and_existing_state()
    {
        var desired = NewCf.Data("Managed", "managed-id", 7) with
        {
            IncludeCustomFormatWhenRenaming = true,
        };
        var managedMapping = NewSyncState.Mapping("managed-id", "Managed", 7);
        var orphanMapping = NewSyncState.Mapping("orphan-id", "Orphan", 8);
        var harness = CreateHarness(
            [new PlannedCustomFormat(desired)],
            [NewCf.Data("Managed", "", 7), NewCf.Data("Orphan", "", 8)],
            [managedMapping, orphanMapping],
            deleteOldCustomFormats: true
        );
        var exception = await CreateApiException(HttpStatusCode.BadRequest);
        harness.Api.UpdateCustomFormat(desired, default).Returns(Task.FromException(exception));
        harness.Api.DeleteCustomFormat(8, default).Returns(Task.FromException(exception));

        var compute = await Persist(harness);

        compute.Status.Should().Be(SyncResultStatus.Failed);
        compute.Resources.Should().ContainSingle();
        compute.Deletes.Should().ContainSingle();
        compute.Outcomes.OfType<CustomFormatUpdateRejectedOutcome>().Should().ContainSingle();
        compute.Outcomes.OfType<CustomFormatDeleteRejectedOutcome>().Should().ContainSingle();
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

        await Compute(harness);
        var act = () => Persist(harness);

        await act.Should().ThrowAsync<ApiException>();
        harness.StatePersister.DidNotReceive().Save(harness.State);
    }

    [Test]
    public async Task Preview_returns_results_without_service_or_state_writes()
    {
        var cf = NewCf.Data("Preview", "trash-id");
        var harness = CreateHarness([new PlannedCustomFormat(cf)], []);
        var executor = NewPipeline.Executor(PipelineType.CustomFormat, harness.Sut);
        var settings = Substitute.For<ISyncSettings>();
        settings.Preview.Returns(true);

        var results = await executor.Execute(
            settings,
            NewPipeline.Plan(harness.Plan, harness.PlanOutcomes),
            new PipelineExecutionBuffer(),
            CancellationToken.None
        );

        results
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<CustomFormatPipelineResult>()
            .Which.Resources.Should()
            .ContainSingle();
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

        var plan = new PlannedCustomFormats(planned);

        var log = Substitute.For<ILogger>();
        var logger = new CustomFormatTransactionLogger(log);
        var sut = new CustomFormatSyncOperation(log, api, statePersister, logger, config);
        return new Harness(sut, api, statePersister, state, plan, []);
    }

    private static async Task<CustomFormatPipelineResult> Compute(Harness harness)
    {
        var result = await ((ISyncOperation<PlannedCustomFormats>)harness.Sut).Execute(
            preview: true,
            harness.Plan,
            harness.PlanOutcomes,
            _ => { },
            CancellationToken.None
        );
        return result.Should().BeOfType<CustomFormatPipelineResult>().Which;
    }

    private static async Task<CustomFormatPipelineResult> Persist(Harness harness)
    {
        var result = await ((ISyncOperation<PlannedCustomFormats>)harness.Sut).Execute(
            preview: false,
            harness.Plan,
            harness.PlanOutcomes,
            _ => { },
            CancellationToken.None
        );
        return result.Should().BeOfType<CustomFormatPipelineResult>().Which;
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
        PlannedCustomFormats Plan,
        List<SyncOutcome> PlanOutcomes
    );
}
