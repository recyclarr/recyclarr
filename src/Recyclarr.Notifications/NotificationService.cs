using System.Diagnostics.Metrics;
using System.Text;
using Flurl.Http;
using Recyclarr.Common.Extensions;
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
    IReadOnlyCollection<IPresentableNotification> presentableNotifications)
{
    public void AddInstanceMetrics(string instanceName, IReadOnlyCollection<IPresentableNotification> metrics)
    {
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
            log.Error(e, "Failed to send notification");
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

public sealed class NotificationScope : IDisposable
{
    private readonly ILogger _log;
    private readonly MeterListener _meterListener = new();

    public NotificationScope(ILogger log)
    {
        _log = log;
        _meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name.StartsWithIgnoreCase("recyclarr."))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        _meterListener.SetMeasurementEventCallback<int>(OnMeasurementRecorded);
    }

    private void OnMeasurementRecorded(
        Instrument instrument,
        int measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state)
    {
        _log.Debug("Measurement for {Instrument} with value {value}", instrument.Name, measurement);
    }

    public void ObtainCapturedMetrics()
    {
        // TODO implement
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        _meterListener.Dispose();
    }
}
