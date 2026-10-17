using System.Collections.ObjectModel;

namespace Recyclarr.Server.Sync.Notifications.Apprise.Dto;

internal record AppriseNotification
{
    public string? Body { get; init; }
    public string? Title { get; init; }
    public AppriseMessageType? Type { get; init; }
    public AppriseMessageFormat? Format { get; init; }
}

internal sealed record AppriseStatefulNotification : AppriseNotification
{
    public string? Tag { get; init; }
}

internal sealed record AppriseStatelessNotification : AppriseNotification
{
    public Collection<string> Urls { get; init; } = [];
}
