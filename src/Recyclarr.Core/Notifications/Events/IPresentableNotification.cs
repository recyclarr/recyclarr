namespace Recyclarr.Notifications.Events;

public interface IPresentableNotification
{
    public string Category { get; }
    public string Render();
}
