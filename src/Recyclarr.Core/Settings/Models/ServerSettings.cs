namespace Recyclarr.Settings.Models;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record ServerSettings
{
    public int Port { get; init; } = 7982;
    public string BindAddress { get; init; } = "localhost";
}
