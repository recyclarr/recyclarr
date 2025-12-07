using System.Globalization;
using System.Text;
using Autofac.Features.Indexed;
using Flurl.Http;
using Recyclarr.Notifications.Apprise;
using Recyclarr.Notifications.Apprise.Dto;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;
using Recyclarr.Sync;
using Recyclarr.Sync.Events;

namespace Recyclarr.Notifications;

public sealed class NotificationService(
    ILogger log,
    IIndex<AppriseMode, IAppriseNotificationApiService> apiFactory,
    ISettings<NotificationSettings> settings,
    SyncEventStorage eventStorage,
    VerbosityOptions verbosity
)
{
    private const string NoInstance = "[no instance]";
    private readonly AppriseNotificationSettings? _settings = settings.Value.Apprise;

    public async Task SendNotification(bool succeeded)
    {
        // If the user didn't configure notifications, exit early and do nothing.
        if (_settings is null)
        {
            log.Debug(
                "Notification settings are not present, so this notification will not be sent"
            );
            return;
        }

        var messageType = succeeded ? AppriseMessageType.Success : AppriseMessageType.Failure;
        var body = BuildNotificationBody();
        await SendAppriseNotification(succeeded, body, messageType);
    }

    private async Task SendAppriseNotification(
        bool succeeded,
        string body,
        AppriseMessageType messageType
    )
    {
        if (string.IsNullOrEmpty(body) && !verbosity.SendEmpty)
        {
            log.Debug("Skipping notification because the body is empty");
            return;
        }

        // Apprise doesn't like empty bodies, so the hyphens are there in case there are no notifications to render.
        // This also doesn't look too bad because it creates some separation between the title and the content.
        body = "---\n" + body.Trim();

        try
        {
            var api = apiFactory[_settings!.Mode!.Value];

            await api.Notify(
                _settings!,
                payload =>
                    payload with
                    {
                        Title = $"Recyclarr Sync {(succeeded ? "Completed" : "Failed")}",
                        Body = body,
                        Type = messageType,
                        Format = AppriseMessageFormat.Markdown,
                    }
            );
        }
        catch (FlurlHttpException e)
        {
            log.Error(e, "Failed to send notification");
        }
    }

    private string BuildNotificationBody()
    {
        var body = new StringBuilder();

        // Group by instance, with [no instance] first, then named instances alphabetically
        var eventsByInstance = eventStorage
            .Events.GroupBy(e => e.InstanceName ?? NoInstance)
            .OrderBy(g => g.Key == NoInstance ? 0 : 1)
            .ThenBy(g => g.Key);

        foreach (var instanceGroup in eventsByInstance)
        {
            RenderInstanceEvents(body, instanceGroup.Key, instanceGroup);
        }

        return body.ToString();
    }

    private void RenderInstanceEvents(
        StringBuilder body,
        string instanceName,
        IEnumerable<SyncEvent> events
    )
    {
        if (instanceName == NoInstance)
        {
            body.AppendLine("### General");
        }
        else
        {
            body.AppendLine(CultureInfo.InvariantCulture, $"### Instance: `{instanceName}`");
        }

        body.AppendLine();

        var eventList = events.ToList();

        // Render completion counts (Information category)
        var completionEvents = eventList.OfType<CompletionEvent>().ToList();
        if (completionEvents.Count > 0 && verbosity.SendInfo)
        {
            body.AppendLine("Information:");
            body.AppendLine();
            foreach (var evt in completionEvents)
            {
                var description = GetPipelineDescription(evt.Pipeline);
                body.AppendLine(CultureInfo.InvariantCulture, $"- {description}: {evt.Count}");
            }

            body.AppendLine();
        }

        // Render errors
        var errors = eventList
            .OfType<DiagnosticEvent>()
            .Where(e => e.Type == DiagnosticType.Error)
            .ToList();
        if (errors.Count > 0)
        {
            body.AppendLine("Errors:");
            body.AppendLine();
            foreach (var evt in errors)
            {
                body.AppendLine(CultureInfo.InvariantCulture, $"- {evt.Message}");
            }

            body.AppendLine();
        }

        // Render warnings (including deprecations)
        var warnings = eventList
            .OfType<DiagnosticEvent>()
            .Where(e => e.Type is DiagnosticType.Warning or DiagnosticType.Deprecation)
            .ToList();
        if (warnings.Count > 0)
        {
            body.AppendLine("Warnings:");
            body.AppendLine();
            foreach (var evt in warnings)
            {
                var prefix = evt.Type == DiagnosticType.Deprecation ? "[DEPRECATED] " : "";
                body.AppendLine(CultureInfo.InvariantCulture, $"- {prefix}{evt.Message}");
            }

            body.AppendLine();
        }
    }

    private static string GetPipelineDescription(PipelineType? pipeline)
    {
        return pipeline switch
        {
            PipelineType.CustomFormat => "Custom Formats Synced",
            PipelineType.QualityProfile => "Quality Profiles Synced",
            PipelineType.QualitySize => "Quality Sizes Synced",
            PipelineType.MediaNaming => "Media Naming Synced",
            _ => "Items Synced",
        };
    }
}
