namespace Recyclarr.Cli.Console;

/// <summary>
/// Recognizes Recyclarr 8 command lines whose command or option was removed in Recyclarr 9, so the
/// user gets the replacement instead of a generic unknown-command error.
/// </summary>
/// <remarks>
/// Matching happens on raw arguments before command parsing because strict parsing would reject
/// the removed options (for example <c>config create --template</c>) before any command runs.
/// </remarks>
internal static class RemovedCommands
{
    private const string UpgradeGuide = "https://recyclarr.dev/guide/upgrade-guide/v9.0/";

    private static readonly (string[] Prefix, string Message)[] Commands =
    [
        (
            ["config", "create"],
            "The `config create` command was removed; copy a template from "
                + "https://recyclarr.dev/guide/guide-configs/ into your configuration directory"
                + $". See {UpgradeGuide}#config-commands-removed"
        ),
        (
            ["config", "list"],
            "The `config list` commands were removed; templates are listed at "
                + "https://recyclarr.dev/guide/guide-configs/"
                + $". See {UpgradeGuide}#config-commands-removed"
        ),
        (
            ["migrate"],
            "The `migrate` command was removed; migrations run automatically when the server "
                + $"starts. See {UpgradeGuide}#migrate-removed"
        ),
        (
            ["state", "repair"],
            "The `state repair` command was removed; sync now adopts existing resources "
                + $"automatically. See {UpgradeGuide}#state-repair-removed"
        ),
    ];

    private const string SyncConfigMessage =
        "The `--config` option of `sync` was removed; move your configuration files into the "
        + $"`configs` directory. See {UpgradeGuide}#sync-config-removed";

    /// <summary>
    /// Returns guidance for a removed command line, or null when the arguments use nothing removed.
    /// </summary>
    public static string? Find(IReadOnlyList<string> args)
    {
        if (args is ["sync", ..] && args.Skip(1).Any(IsConfigOption))
        {
            return SyncConfigMessage;
        }

        return Commands
            .Where(c => args.Take(c.Prefix.Length).SequenceEqual(c.Prefix))
            .Select(c => c.Message)
            .FirstOrDefault();
    }

    private static bool IsConfigOption(string arg)
    {
        return arg is "-c" or "--config" || arg.StartsWith("--config=", StringComparison.Ordinal);
    }
}
