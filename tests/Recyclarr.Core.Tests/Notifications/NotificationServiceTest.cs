using Recyclarr.Notifications;
using Recyclarr.Notifications.Apprise;
using Recyclarr.Notifications.Apprise.Dto;
using Recyclarr.Settings.Models;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;
using Recyclarr.TestLibrary;

namespace Recyclarr.Core.Tests.Notifications;

[TestFixture]
internal sealed class NotificationServiceTest
{
    private SyncRunScope _scope = null!;
    private IAppriseNotificationApiService _api = null!;
    private AppriseNotification? _capturedNotification;

    private NotificationService CreateSut(VerbosityOptions verbosity)
    {
        _scope = new SyncRunScope();
        _api = Substitute.For<IAppriseNotificationApiService>();

        _api.Notify(default!).ReturnsForAnyArgs(Task.CompletedTask);
        _api.When(x => x.Notify(Arg.Any<Func<AppriseNotification, AppriseNotification>>()))
            .Do(x =>
            {
                var builder = x.Arg<Func<AppriseNotification, AppriseNotification>>();
                _capturedNotification = builder(new AppriseNotification());
            });

        return new NotificationService(new TestableLogger(), _api, _scope, verbosity);
    }

    private void PublishSucceeded(string instance, PipelineType type, int count)
    {
        _scope.Publish(new PipelineEvent(instance, type, PipelineProgressStatus.Succeeded, count));
    }

    private void PublishDiagnostic(string? instance, SyncDiagnosticLevel level, string message)
    {
        _scope.Publish(new SyncDiagnosticEvent(instance, level, message));
    }

    [Test]
    public async Task All_zero_counts_suppresses_notification()
    {
        using var sut = CreateSut(VerbosityOptions.From(NotificationVerbosity.Normal));

        PublishSucceeded("Radarr", PipelineType.CustomFormat, 0);
        PublishSucceeded("Radarr", PipelineType.QualityProfile, 0);
        PublishSucceeded("Radarr", PipelineType.QualitySize, 0);
        PublishSucceeded("Radarr", PipelineType.MediaNaming, 0);

        await sut.SendNotification();

        await _api.DidNotReceiveWithAnyArgs().Notify(default!);
    }

    [Test]
    public async Task Mix_of_zero_and_nonzero_counts_sends_only_nonzero()
    {
        using var sut = CreateSut(VerbosityOptions.From(NotificationVerbosity.Normal));

        PublishSucceeded("Radarr", PipelineType.CustomFormat, 5);
        PublishSucceeded("Radarr", PipelineType.QualityProfile, 0);
        PublishSucceeded("Radarr", PipelineType.QualitySize, 3);
        PublishSucceeded("Radarr", PipelineType.MediaNaming, 0);

        await sut.SendNotification();

        await _api.ReceivedWithAnyArgs(1).Notify(default!);
        _capturedNotification!.Body.Should().Contain("Custom Formats Synced: 5");
        _capturedNotification.Body.Should().Contain("Quality Sizes Synced: 3");
        _capturedNotification.Body.Should().NotContain("Quality Profiles Synced");
        _capturedNotification.Body.Should().NotContain("Media Naming Synced");
    }

    [Test]
    public async Task All_nonzero_counts_includes_all_stats()
    {
        using var sut = CreateSut(VerbosityOptions.From(NotificationVerbosity.Normal));

        PublishSucceeded("Radarr", PipelineType.CustomFormat, 2);
        PublishSucceeded("Radarr", PipelineType.QualityProfile, 1);
        PublishSucceeded("Radarr", PipelineType.QualitySize, 4);
        PublishSucceeded("Radarr", PipelineType.MediaNaming, 1);

        await sut.SendNotification();

        await _api.ReceivedWithAnyArgs(1).Notify(default!);
        _capturedNotification!.Body.Should().Contain("Custom Formats Synced: 2");
        _capturedNotification.Body.Should().Contain("Quality Profiles Synced: 1");
        _capturedNotification.Body.Should().Contain("Quality Sizes Synced: 4");
        _capturedNotification.Body.Should().Contain("Media Naming Synced: 1");
    }

    [Test]
    public async Task Diagnostics_present_with_zero_counts_sends_notification()
    {
        using var sut = CreateSut(VerbosityOptions.From(NotificationVerbosity.Normal));

        PublishSucceeded("Radarr", PipelineType.CustomFormat, 0);
        PublishDiagnostic("Radarr", SyncDiagnosticLevel.Error, "Something went wrong");

        await sut.SendNotification();

        await _api.ReceivedWithAnyArgs(1).Notify(default!);
        _capturedNotification!.Body.Should().Contain("Something went wrong");
    }

    [Test]
    public async Task Detailed_verbosity_sends_even_when_empty()
    {
        using var sut = CreateSut(VerbosityOptions.From(NotificationVerbosity.Detailed));

        PublishSucceeded("Radarr", PipelineType.CustomFormat, 0);

        await sut.SendNotification();

        await _api.ReceivedWithAnyArgs(1).Notify(default!);
    }

    [Test]
    public async Task Minimal_verbosity_suppresses_info_stats()
    {
        using var sut = CreateSut(VerbosityOptions.From(NotificationVerbosity.Minimal));

        PublishSucceeded("Radarr", PipelineType.CustomFormat, 5);
        PublishDiagnostic("Radarr", SyncDiagnosticLevel.Warning, "Deprecated feature");

        await sut.SendNotification();

        await _api.ReceivedWithAnyArgs(1).Notify(default!);
        _capturedNotification!.Body.Should().NotContain("Custom Formats Synced");
        _capturedNotification.Body.Should().Contain("Deprecated feature");
    }
}
