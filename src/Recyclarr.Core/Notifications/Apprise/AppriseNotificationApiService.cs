using Flurl.Http;
using Recyclarr.Notifications.Apprise.Dto;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.Notifications.Apprise;

internal class AppriseNotificationApiService(
    IAppriseRequestBuilder api,
    ISettings<NotificationSettings> settings
) : IAppriseNotificationApiService
{
    public async Task Notify(Func<AppriseNotification, AppriseNotification> notificationBuilder)
    {
        // Guaranteed non-null: only constructed when INotificationService factory confirms Apprise is configured
        var apprise = settings.Value.Apprise!;

        var (notification, request) = apprise.Mode switch
        {
            AppriseMode.Stateful => (
                notificationBuilder(new AppriseStatefulNotification { Tag = apprise.Tags }),
                api.Request("notify", apprise.Key)
            ),
            AppriseMode.Stateless => (
                notificationBuilder(new AppriseStatelessNotification { Urls = apprise.Urls }),
                api.Request("notify")
            ),
            _ => throw new InvalidOperationException($"Unsupported Apprise mode: {apprise.Mode}"),
        };

        await request.PostJsonAsync(notification);
    }
}
