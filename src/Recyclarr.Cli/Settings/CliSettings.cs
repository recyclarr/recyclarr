namespace Recyclarr.Cli.Settings;

// Settings from cli.yml. The file is optional; its absence means every default applies.
internal sealed record CliSettings
{
    // Address of an already-running server. Its presence selects centralized mode: commands talk
    // to that server. Unset, each command launches a private server for its duration (ADR-010).
    public Uri? ServerBaseUrl { get; init; }
}
