using System.Collections.ObjectModel;

namespace Recyclarr.Settings;

public record TrashRepository : IRepositorySettings
{
    public Uri CloneUrl { get; [UsedImplicitly] init; } = new("https://github.com/TRaSH-Guides/Guides.git");
    public string Branch { get; [UsedImplicitly] init; } = "master";
    public string? Sha1 { get; [UsedImplicitly] init; }
}

public record ConfigTemplateRepository : IRepositorySettings
{
    public Uri CloneUrl { get; [UsedImplicitly] init; } = new("https://github.com/recyclarr/config-templates.git");
    public string Branch { get; [UsedImplicitly] init; } = "master";
    public string? Sha1 { get; [UsedImplicitly] init; }
}

public record LogJanitorSettings
{
    public int MaxFiles { get; [UsedImplicitly] init; } = 20;
}

public record Repositories
{
    public TrashRepository TrashGuides { get; [UsedImplicitly] init; } = new();
    public ConfigTemplateRepository ConfigTemplates { get; [UsedImplicitly] init; } = new();
}

public record RecyclarrSettings
{
    public Repositories Repositories { get; [UsedImplicitly] init; } = new();
    public bool EnableSslCertificateValidation { get; [UsedImplicitly] init; } = true;
    public LogJanitorSettings LogJanitor { get; [UsedImplicitly] init; } = new();
    public string? GitPath { get; [UsedImplicitly] init; }
    public NotificationSettings? Notifications { get; [UsedImplicitly] init; }
}

public record NotificationSettings
{
    public AppriseNotificationSettings? Apprise { get; [UsedImplicitly] init; }
}

public record AppriseNotificationSettings
{
    public AppriseMode? Mode { get; [UsedImplicitly] init; }
    public Uri BaseUrl { get; [UsedImplicitly] init; } = new("about:empty");
    public string Key { get; [UsedImplicitly] init; } = "";
    public string Tags { get; [UsedImplicitly] init; } = "";
    public Collection<string> Urls { get; [UsedImplicitly] init; } = [];
}

public enum AppriseMode
{
    Stateful,
    Stateless
}
