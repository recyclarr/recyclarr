using Flurl.Http;
using Recyclarr.Notifications.Apprise.Dto;
using Recyclarr.Settings.Models;

namespace Recyclarr.Notifications.Apprise;

public class AppriseStatelessNotificationApiService(IAppriseRequestBuilder api)
    : IAppriseNotificationApiService
{
    public async Task Notify(
        AppriseNotificationSettings settings,
        Func<AppriseNotification, AppriseNotification> notificationBuilder
    )
    {
        var notification = notificationBuilder(
            new AppriseStatelessNotification { Urls = settings.Urls }
        );

        await api.Request("notify").PostJsonAsync(notification);
    }
}
