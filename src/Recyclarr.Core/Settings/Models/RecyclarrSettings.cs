namespace Recyclarr.Settings.Models;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record RecyclarrSettings
{
    public IReadOnlyCollection<ResourceProvider> ResourceProviders { get; init; } = [];
    public bool EnableSslCertificateValidation { get; init; } = true;
    public LogJanitorSettings LogJanitor { get; init; } = new();
    public string? GitPath { get; init; }
    public NotificationSettings Notifications { get; init; } = new();
}
