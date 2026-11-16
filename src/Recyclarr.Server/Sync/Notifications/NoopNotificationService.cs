using Recyclarr.Sync.Results;

namespace Recyclarr.Server.Sync.Notifications;

internal sealed class NoopNotificationService : INotificationService
{
    public Task SendNotification(SyncRunResult result) => Task.CompletedTask;
}
