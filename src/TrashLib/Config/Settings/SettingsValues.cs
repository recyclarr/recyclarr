namespace TrashLib.Config.Settings;

public record TrashRepository
{
    public string CloneUrl { get; init; } = "https://github.com/TRaSH-/Guides.git";
}

public record SettingsValues
{
    public TrashRepository Repository { get; init; } = new();
}
