using System.Collections.ObjectModel;

namespace Recyclarr.Notifications.Apprise.Dto;

[UsedImplicitly(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.WithMembers)]
public record AppriseNotification
{
    public string? Body { get; init; }
    public string? Title { get; init; }
    public AppriseMessageType? Type { get; init; }
    public AppriseMessageFormat? Format { get; init; }
}

[UsedImplicitly(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.WithMembers)]
public record AppriseStatefulNotification : AppriseNotification
{
    public string? Tag { get; init; }
}

[UsedImplicitly(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.WithMembers)]
public record AppriseStatelessNotification : AppriseNotification
{
    public Collection<string> Urls { get; init; } = [];
}
