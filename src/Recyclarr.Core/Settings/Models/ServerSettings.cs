namespace Recyclarr.Settings.Models;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record ServerSettings
{
    public int Port { get; init; } = 7982;
    public string BindAddress { get; init; } = "localhost";

    // Address of an already-running server. Its presence is what selects centralized mode: set,
    // commands talk to that server; unset, they launch a private one for the duration of the
    // command (ADR-010).
    public Uri? BaseUrl { get; init; }
}
