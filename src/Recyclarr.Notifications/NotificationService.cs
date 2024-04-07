using System.Diagnostics.Metrics;
using System.Text;
using Flurl.Http;
using Recyclarr.Http;
using Recyclarr.Notifications.Apprise;
using Recyclarr.Notifications.Apprise.Dto;
using Recyclarr.Notifications.Events;
using Recyclarr.Settings;
using Serilog;

namespace Recyclarr.Notifications;

public sealed class NotificationService(
    ILogger log,
    IAppriseNotificationApiService apprise,
    ISettingsProvider settingsProvider,
    IReadOnlyCollection<IPresentableNotification> presentableNotifications,
    MeterListener meterListener)
{
    public void BeginCollecting(string instanceName)
    {
        var meter = meterFactory.Create(instanceName);
        meter.
        if (_activeInstanceName is not null)
        {
            RenderInstanceEvents(_activeInstanceName, presentableNotifications);
        }

        _activeInstanceName = instanceName;
    }

    public async Task SendNotification(bool succeeded)
    {
        // If the user didn't configure notifications, exit early and do nothing.
        if (settingsProvider.Settings.Notifications is null)
        {
            log.Debug("Notification settings are not present, so this notification will not be sent");
            return;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(_activeInstanceName);

        var body = RenderInstanceEvents(_activeInstanceName, presentableNotifications);

        var messageType = AppriseMessageType.Success;
        if (!succeeded)
        {
            messageType = AppriseMessageType.Failure;
        }

        try
        {
            await apprise.Notify("apprise", new AppriseNotification
            {
                Title = $"Recyclarr Sync {(succeeded ? "Completed" : "Failed")}",
                Body = body,
                Type = messageType,
                Format = AppriseMessageFormat.Markdown
            });
        }
        catch (FlurlHttpException e)
        {
            log.Error("Failed to send notification: {Msg}", e.SanitizedExceptionMessage());
        }
    }

    private static string RenderInstanceEvents(
        string instanceName,
        IEnumerable<IPresentableNotification> notifications)
    {
        var body = new StringBuilder($"### Instance: `{instanceName}`\n");

        foreach (var notification in notifications.GroupBy(x => x.Category))
        {
            body.AppendLine(
                $"""
                 {notification.Key}:
                 {string.Join('\n', notification.Select(x => x.Render()))}

                 """);
        }

        return body.ToString();
    }
}
