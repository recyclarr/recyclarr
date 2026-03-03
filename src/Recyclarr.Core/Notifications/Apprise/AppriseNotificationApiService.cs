using Recyclarr.Notifications.Apprise.Dto;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.Notifications.Apprise;

internal class AppriseNotificationApiService(
    IAppriseApi api,
    ISettings<NotificationSettings> settings
) : IAppriseNotificationApiService
{
    public async Task Notify(Func<AppriseNotification, AppriseNotification> notificationBuilder)
    {
        // Guaranteed non-null: only constructed when INotificationService factory confirms Apprise is configured
        var apprise = settings.Value.Apprise!;

        switch (apprise.Mode)
        {
            case AppriseMode.Stateful:
                var stateful = (AppriseStatefulNotification)notificationBuilder(
                    new AppriseStatefulNotification { Tag = apprise.Tags }
                );
                await api.Notify(apprise.Key, stateful);
                break;

            case AppriseMode.Stateless:
                var stateless = (AppriseStatelessNotification)notificationBuilder(
                    new AppriseStatelessNotification { Urls = apprise.Urls }
                );
                await api.Notify(stateless);
                break;

            default:
                throw new InvalidOperationException($"Unsupported Apprise mode: {apprise.Mode}");
        }
    }
}
