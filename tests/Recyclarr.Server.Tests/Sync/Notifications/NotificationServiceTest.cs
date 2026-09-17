using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Server.Sync.Notifications;
using Recyclarr.Server.Sync.Notifications.Apprise;
using Recyclarr.Server.Sync.Notifications.Apprise.Dto;
using Recyclarr.Settings.Models;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using Recyclarr.TestLibrary;
using Recyclarr.TrashGuide;

namespace Recyclarr.Server.Tests.Sync.Notifications;

[TestFixture]
internal sealed class NotificationServiceTest
{
    private IAppriseNotificationApiService _api = null!;
    private AppriseNotification? _capturedNotification;

    private NotificationService CreateSut(VerbosityOptions verbosity)
    {
        _api = Substitute.For<IAppriseNotificationApiService>();

        _api.Notify(default!).ReturnsForAnyArgs(Task.CompletedTask);
        _api.When(x => x.Notify(Arg.Any<Func<AppriseNotification, AppriseNotification>>()))
            .Do(x =>
            {
                var builder = x.Arg<Func<AppriseNotification, AppriseNotification>>();
                builder.Should().NotBeNull();
                _capturedNotification = builder(new AppriseNotification());
            });

        return new NotificationService(new TestableLogger(), _api, verbosity);
    }

    [Test]
    public async Task Successful_result_without_reportable_content_suppresses_notification()
    {
        var sut = CreateSut(VerbosityOptions.From(NotificationVerbosity.Normal));

        await sut.SendNotification(new SyncRunResult([]));

        await _api.DidNotReceiveWithAnyArgs().Notify(default!);
    }

    [Test]
    public async Task Detailed_verbosity_sends_even_when_result_has_no_reportable_content()
    {
        var sut = CreateSut(VerbosityOptions.From(NotificationVerbosity.Detailed));

        await sut.SendNotification(new SyncRunResult([]));

        await _api.ReceivedWithAnyArgs(1).Notify(default!);
        _capturedNotification.Should().NotBeNull();
        _capturedNotification.Title.Should().Be("Recyclarr Sync Completed");
        _capturedNotification.Type.Should().Be(AppriseMessageType.Success);
    }

    [Test]
    public async Task Result_status_and_semantic_context_determine_failure_notification()
    {
        var sut = CreateSut(VerbosityOptions.From(NotificationVerbosity.Minimal));
        var result = new SyncRunResult(
            [
                new SyncInstanceResult(
                    "movies",
                    SupportedServices.Radarr,
                    [
                        new CustomFormatPipelineResult(
                            completedResources: 0,
                            incompleteResources: 1,
                            [new CustomFormatReferenceMismatchOutcome("missing-cf")],
                            []
                        ),
                    ],
                    planningOutcomes:
                    [
                        new CustomFormatGroupSelectReferenceMismatchPlanningOutcome(
                            "group-id",
                            "selected-cf"
                        ),
                        new CustomFormatGroupRequiredItemSelectedPlanningOutcome(
                            "group-id",
                            "required-cf"
                        ),
                    ]
                ),
                new SyncInstanceResult(
                    "offline",
                    SupportedServices.Sonarr,
                    [],
                    new ServiceUnavailableFailure(),
                    fault: new SyncFault("instance-fault")
                ),
            ],
            new SyncFault("fault-reference")
        );

        await sut.SendNotification(result);

        await _api.ReceivedWithAnyArgs(1).Notify(default!);
        _capturedNotification.Should().NotBeNull();
        _capturedNotification.Title.Should().Be("Recyclarr Sync Failed");
        _capturedNotification.Type.Should().Be(AppriseMessageType.Failure);
        _capturedNotification
            .Body.Should()
            .ContainAll(
                "fault-reference",
                "instance-fault",
                "movies",
                "group-id",
                "selected-cf",
                "required-cf",
                "missing-cf",
                "offline",
                "service is unavailable"
            );
    }

    [Test]
    public async Task Normal_verbosity_reports_changed_resource_counts_without_item_details()
    {
        var sut = CreateSut(VerbosityOptions.From(NotificationVerbosity.Normal));
        var result = CreateCustomFormatResult();

        await sut.SendNotification(result);

        await _api.ReceivedWithAnyArgs(1).Notify(default!);
        _capturedNotification.Should().NotBeNull();
        _capturedNotification.Body.Should().Contain("Custom Formats Changed: 3");
        _capturedNotification.Body.Should().NotContain("- Created:");
        _capturedNotification.Body.Should().NotContain("- Updated:");
        _capturedNotification.Body.Should().NotContain("- Deleted:");
    }

    [Test]
    public async Task Blocked_pipeline_identifies_its_dependency()
    {
        var sut = CreateSut(VerbosityOptions.From(NotificationVerbosity.Minimal));
        var blocked = new QualityProfilePipelineResult(0, 0, [], []).WithStatus(
            SyncResultStatus.Blocked,
            PipelineType.CustomFormat
        );
        var result = new SyncRunResult([
            new SyncInstanceResult("movies", SupportedServices.Radarr, [blocked]),
        ]);

        await sut.SendNotification(result);

        _capturedNotification.Should().NotBeNull();
        _capturedNotification.Body.Should().Contain("Quality Profiles blocked by Custom Formats");
    }

    [Test]
    public async Task Verbose_verbosity_includes_bounded_resource_details()
    {
        var sut = CreateSut(VerbosityOptions.From(NotificationVerbosity.Verbose));
        var result = CreateCustomFormatResult([
            .. Enumerable
                .Range(1, 25)
                .Select(i =>
                    (CustomFormatDelta)
                        new CustomFormatUpdateDelta(
                            new CustomFormatIdentity($"id-{i}", $"CF{i:D2}"),
                            new CustomFormatSourceInfo(
                                CfSource.CfGroupExplicit,
                                "Group",
                                CfInclusionReason.Selected,
                                []
                            ),
                            []
                        )
                ),
        ]);

        await sut.SendNotification(result);

        await _api.ReceivedWithAnyArgs(1).Notify(default!);
        _capturedNotification.Should().NotBeNull();
        _capturedNotification.Body.Should().Contain("- Updated: CF01, CF02, CF03");
        _capturedNotification.Body.Should().Contain("(and 5 more)");
        _capturedNotification.Body.Should().NotContain("CF21");
    }

    private static SyncRunResult CreateCustomFormatResult(
        IReadOnlyList<CustomFormatDelta>? deltas = null
    )
    {
        deltas ??=
        [
            new CustomFormatCreateDelta(
                new CustomFormatIdentity("created", "NewCF"),
                new CustomFormatSourceInfo(
                    CfSource.CfGroupExplicit,
                    "Group",
                    CfInclusionReason.Selected,
                    []
                )
            ),
            new CustomFormatUpdateDelta(
                new CustomFormatIdentity("updated", "UpdatedCF"),
                new CustomFormatSourceInfo(
                    CfSource.CfGroupExplicit,
                    "Group",
                    CfInclusionReason.Selected,
                    []
                ),
                []
            ),
            new CustomFormatDeleteDelta(new CustomFormatIdentity("deleted", "DeletedCF")),
        ];

        return new SyncRunResult([
            new SyncInstanceResult(
                "movies",
                SupportedServices.Radarr,
                [new CustomFormatPipelineResult(deltas.Count, 0, [], deltas)]
            ),
        ]);
    }
}
