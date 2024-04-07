namespace Recyclarr.Notifications.Apprise.Dto;

public record AppriseNotification
{
    public required string Body { get; init; }
    public string? Title { get; init; }
    public AppriseMessageType? Type { get; init; }
    public AppriseMessageFormat? Format { get; init; }
    public string? Tag { get; init; }
}
