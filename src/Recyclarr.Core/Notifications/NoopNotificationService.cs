namespace Recyclarr.Notifications;

internal sealed class NoopNotificationService : INotificationService
{
    public Task SendNotification() => Task.CompletedTask;
}
