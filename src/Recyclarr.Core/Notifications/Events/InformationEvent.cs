namespace Recyclarr.Notifications.Events;

public record InformationEvent(string Description) : IPresentableNotification
{
    public string? Statistic { get; init; }

    public string Category => "Information";

    public string Render() => $"- {Description}{(Statistic is null ? "" : $": {Statistic}")}";
}
