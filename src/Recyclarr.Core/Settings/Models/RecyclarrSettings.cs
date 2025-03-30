namespace Recyclarr.Settings.Models;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record RecyclarrSettings
{
    // Replaced by DataSources
    public Repositories? Repositories { get; init; }
    public DataSourceSettings DataSources { get; init; } = new();
    public bool EnableSslCertificateValidation { get; init; } = true;
    public LogJanitorSettings LogJanitor { get; init; } = new();
    public string? GitPath { get; init; }
    public NotificationSettings Notifications { get; init; } = new();
}
