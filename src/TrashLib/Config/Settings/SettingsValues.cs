namespace TrashLib.Config.Settings;

public record TrashRepository
{
    public string CloneUrl { get; init; } = "https://github.com/TRaSH-/Guides.git";
    public string Branch { get; init; } = "master";
    public string? Sha1 { get; init; }
}

public record LogJanitorSettings
{
    public int MaxFiles { get; init; } = 20;
}

public record SettingsValues
{
    public TrashRepository Repository { get; init; } = new();
    public bool EnableSslCertificateValidation { get; init; } = true;
    public LogJanitorSettings LogJanitor { get; init; } = new();
}
