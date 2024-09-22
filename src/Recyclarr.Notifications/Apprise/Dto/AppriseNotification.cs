using System.Collections.ObjectModel;

namespace Recyclarr.Notifications.Apprise.Dto;

public record AppriseNotification
{
    public string? Body { get; init; }
    public string? Title { get; init; }
    public AppriseMessageType? Type { get; init; }
    public AppriseMessageFormat? Format { get; init; }
}

public record AppriseStatefulNotification : AppriseNotification
{
    public string? Tag { get; init; }
}

public record AppriseStatelessNotification : AppriseNotification
{
    public Collection<string> Urls { get; init; } = [];
}
