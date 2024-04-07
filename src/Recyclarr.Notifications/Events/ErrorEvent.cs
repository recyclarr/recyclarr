namespace Recyclarr.Notifications.Events;

public record ErrorEvent(string Message) : IPresentableNotification
{
    public string Category => "Errors";
    public string Render() => $"- {Message}";
}
