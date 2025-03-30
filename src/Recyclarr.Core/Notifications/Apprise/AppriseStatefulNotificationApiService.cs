using Flurl.Http;
using Recyclarr.Notifications.Apprise.Dto;
using Recyclarr.Settings.Models;

namespace Recyclarr.Notifications.Apprise;

public class AppriseStatefulNotificationApiService(IAppriseRequestBuilder api)
    : IAppriseNotificationApiService
{
    public async Task Notify(
        AppriseNotificationSettings settings,
        Func<AppriseNotification, AppriseNotification> notificationBuilder
    )
    {
        var notification = notificationBuilder(
            new AppriseStatefulNotification { Tag = settings.Tags }
        );

        await api.Request("notify", settings.Key).PostJsonAsync(notification);
    }
}
