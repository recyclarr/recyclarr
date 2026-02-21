using Recyclarr.Notifications.Apprise.Dto;

namespace Recyclarr.Notifications.Apprise;

public interface IAppriseNotificationApiService
{
    Task Notify(Func<AppriseNotification, AppriseNotification> notificationBuilder);
}
