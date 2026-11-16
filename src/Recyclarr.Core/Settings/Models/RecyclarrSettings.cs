namespace Recyclarr.Settings.Models;

public record RecyclarrSettings
{
    public IReadOnlyCollection<ResourceProvider> ResourceProviders { get; init; } = [];
    public bool EnableSslCertificateValidation { get; init; } = true;
    public LogJanitorSettings LogJanitor { get; init; } = new();
    public string? GitPath { get; init; }
    public NotificationSettings Notifications { get; init; } = new();
    public ServerSettings Server { get; init; } = new();
}
