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
}
