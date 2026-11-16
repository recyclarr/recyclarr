namespace Recyclarr.Settings.Models;

public record LogJanitorSettings
{
    public int MaxFiles { get; init; } = 20;
}
