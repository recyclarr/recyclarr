using System.ComponentModel;
using Recyclarr.Cli.Console.Helpers;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

internal class BaseCommandSettings : CommandSettings
{
    [CommandOption("--log [LEVEL]")]
    [EnumDescription<CliLogLevel>("Enable log output mode.")]
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    [DefaultValue(CliLogLevel.Info)]
    public FlagValue<CliLogLevel> LogLevel { get; init; } = null!;

    // Redirected output switches to log mode because progress animations corrupt it. Raw list
    // output is exempt: it exists for pipes, and log mode would discard it.
    public bool IsLogMode =>
        LogLevel.IsSet
        || (System.Console.IsOutputRedirected && this is not ListCommandSettings { Raw: true });

    // When --log is explicit, use the user's chosen level; for auto-detected non-TTY, default to Info
    public CliLogLevel EffectiveLogLevel => LogLevel.IsSet ? LogLevel.Value : CliLogLevel.Info;
}
