namespace Recyclarr.Notifications.Events;

public record ErrorEvent(string Error) : IPresentableNotification
{
    public string Category => "Errors";
    public string Render() => $"- {Error}";
}
