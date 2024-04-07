using System.Reactive.Disposables;
using System.Text;
using Autofac.Features.Indexed;
using Flurl.Http;
using Recyclarr.Common.Extensions;
using Recyclarr.Notifications.Apprise;
using Recyclarr.Notifications.Apprise.Dto;
using Recyclarr.Notifications.Events;
using Recyclarr.Settings;

namespace Recyclarr.Notifications;

public sealed class NotificationService(
    ILogger log,
    IIndex<AppriseMode, IAppriseNotificationApiService> apiFactory,
    ISettings<NotificationSettings> settings,
    NotificationEmitter notificationEmitter)
    : IDisposable
{
    private const string NoInstance = "[no instance]";

    private readonly Dictionary<string, List<IPresentableNotification>> _events = new();
    private readonly CompositeDisposable _eventConnection = new();
    private readonly AppriseNotificationSettings? _settings = settings.OptionalValue?.Apprise;

    private string? _activeInstanceName;

    public void Dispose()
    {
        _eventConnection.Dispose();
    }

    public void SetInstanceName(string instanceName)
    {
        _activeInstanceName = instanceName;
    }

    public void BeginWatchEvents()
    {
        _events.Clear();
        _eventConnection.Clear();
        _eventConnection.Add(notificationEmitter.OnNotification.Subscribe(x =>
        {
            var key = _activeInstanceName ?? NoInstance;
            _events.GetOrCreate(key).Add(x);
        }));
    }

    public async Task SendNotification(bool succeeded)
    {
        // stop receiving events while we build the report
        _eventConnection.Clear();

        // If the user didn't configure notifications, exit early and do nothing.
        if (_settings is null)
        {
            log.Debug("Notification settings are not present, so this notification will not be sent");
            return;
        }

        var messageType = succeeded ? AppriseMessageType.Success : AppriseMessageType.Failure;
        var body = BuildNotificationBody();
        await SendAppriseNotification(succeeded, body, messageType);
    }

    private async Task SendAppriseNotification(bool succeeded, string body, AppriseMessageType messageType)
    {
        try
        {
            var api = apiFactory[_settings!.Mode!.Value];

            await api.Notify(_settings!, payload => payload with
            {
                Title = $"Recyclarr Sync {(succeeded ? "Completed" : "Failed")}",
                Body = body.Trim(),
                Type = messageType,
                Format = AppriseMessageFormat.Markdown
            });
        }
        catch (FlurlHttpException e)
        {
            log.Error(e, "Failed to send notification");
        }
    }

    private string BuildNotificationBody()
    {
        // Apprise doesn't like empty bodies, so the hyphens are there in case there are no notifications to render.
        // This also doesn't look too bad because it creates some separation between the title and the content.
        var body = new StringBuilder("---\n");

        foreach (var (instanceName, notifications) in _events)
        {
            RenderInstanceEvents(body, instanceName, notifications);
        }

        return body.ToString();
    }

    private static void RenderInstanceEvents(
        StringBuilder body,
        string instanceName,
        IEnumerable<IPresentableNotification> notifications)
    {
        if (instanceName == NoInstance)
        {
            body.AppendLine("### General");
        }
        else
        {
            body.AppendLine($"### Instance: `{instanceName}`");
        }

        body.AppendLine();

        var groupedEvents = notifications
            .GroupBy(x => x.Category)
            .ToDictionary(x => x.Key, x => x.ToList());

        foreach (var (category, events) in groupedEvents)
        {
            body.AppendLine(
                $"""
                 {category}:

                 {string.Join('\n', events.Select(x => x.Render()))}

                 """);
        }
    }
}
