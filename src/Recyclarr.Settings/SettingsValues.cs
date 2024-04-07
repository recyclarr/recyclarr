using JetBrains.Annotations;

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

public record SettingsValues
{
    public Repositories Repositories { get; [UsedImplicitly] init; } = new();
    public bool EnableSslCertificateValidation { get; [UsedImplicitly] init; } = true;
    public LogJanitorSettings LogJanitor { get; [UsedImplicitly] init; } = new();
    public string? GitPath { get; [UsedImplicitly] init; }
    public NotificationSettings? Notifications { get; init; }
}

public record NotificationSettings
{
    public AppriseNotificationSettings? Apprise { get; init; }
}

public record AppriseNotificationSettings
{
    public Uri? BaseUrl { get; init; }
    public string? Key { get; init; }
    public string? Tags { get; init; }
}
