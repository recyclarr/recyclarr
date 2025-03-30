using System.Collections.ObjectModel;

namespace Recyclarr.Settings.Models;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record AppriseNotificationSettings
{
    public AppriseMode? Mode { get; init; }
    public Uri BaseUrl { get; init; } = new("about:empty");
    public string Key { get; init; } = "";
    public string Tags { get; init; } = "";
    public Collection<string> Urls { get; init; } = [];
}

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record NotificationSettings
{
    public NotificationVerbosity Verbosity { get; init; } = NotificationVerbosity.Normal;
    public AppriseNotificationSettings? Apprise { get; init; }

    public bool IsConfigured() => Apprise is not null;
}

public enum NotificationVerbosity
{
    Minimal,
    Normal,
    Detailed,
}

public enum AppriseMode
{
    Stateful,
    Stateless,
}
