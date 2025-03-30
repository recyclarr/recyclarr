namespace Recyclarr.Settings.Models;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record LogJanitorSettings
{
    public int MaxFiles { get; init; } = 20;
}
