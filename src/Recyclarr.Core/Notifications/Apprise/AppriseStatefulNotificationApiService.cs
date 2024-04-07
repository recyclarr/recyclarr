using Flurl.Http;
using Recyclarr.Notifications.Apprise.Dto;
using Recyclarr.Settings;

namespace Recyclarr.Notifications.Apprise;

public class AppriseStatefulNotificationApiService(IAppriseRequestBuilder api) : IAppriseNotificationApiService
{
    public async Task Notify(
        AppriseNotificationSettings settings,
        Func<AppriseNotification, AppriseNotification> notificationBuilder)
    {
        if (settings.Key is null)
        {
            throw new ArgumentException("Stateful apprise notifications require the 'key' node");
        }

        var notification = notificationBuilder(new AppriseStatefulNotification
        {
            Tag = settings.Tags
        });

        await api.Request("notify", settings.Key).PostJsonAsync(notification);
    }
}
