using Recyclarr.Notifications.Apprise.Dto;
using Recyclarr.Settings.Models;

namespace Recyclarr.Notifications.Apprise;

public interface IAppriseNotificationApiService
{
    Task Notify(
        AppriseNotificationSettings settings,
        Func<AppriseNotification, AppriseNotification> notificationBuilder
    );
}
