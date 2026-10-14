namespace Recyclarr.Cli.Settings;

/// <summary>
/// Settings from cli.yml. The file is optional; its absence means every default applies.
/// </summary>
internal sealed record CliSettings
{
    /// <summary>
    /// Address of an already-running server. Its presence selects centralized mode: commands talk
    /// to that server. Unset, each command launches a private server for its duration (ADR-010).
    /// </summary>
    public Uri? ServerBaseUrl { get; init; }

    /// <summary>
    /// The server API key sent to the configured server. An ephemeral server ignores it, because
    /// the CLI gives that server a one-time key of its own (ADR-027).
    /// </summary>
    public string? ServerApiKey { get; init; }
}
