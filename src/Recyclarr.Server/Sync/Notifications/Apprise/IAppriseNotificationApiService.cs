using Recyclarr.Server.Sync.Notifications.Apprise.Dto;

namespace Recyclarr.Server.Sync.Notifications.Apprise;

internal interface IAppriseNotificationApiService
{
    Task Notify(Func<AppriseNotification, AppriseNotification> notificationBuilder);
}
