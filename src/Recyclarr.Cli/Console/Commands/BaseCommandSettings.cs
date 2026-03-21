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

    // Log mode activates when explicitly requested or when output is redirected (non-TTY),
    // since Spectre.Console animations produce garbage in piped/redirected output
    public bool IsLogMode => LogLevel.IsSet || System.Console.IsOutputRedirected;

    // When --log is explicit, use the user's chosen level; for auto-detected non-TTY, default to Info
    public CliLogLevel EffectiveLogLevel => LogLevel.IsSet ? LogLevel.Value : CliLogLevel.Info;
}
