namespace Recyclarr.Notifications.Events;

public record WarningEvent(string Message) : IPresentableNotification
{
    public string Category => "Warnings";
    public string Render() => $"- {Message}";
}
