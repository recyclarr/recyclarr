using Recyclarr.Sync.Results;

namespace Recyclarr.Server.Sync.Notifications;

/// <summary>
/// Delivers a notification that presents the completed result of one sync run.
/// </summary>
internal interface INotificationService
{
    Task SendNotification(SyncRunResult result);
}
