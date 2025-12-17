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

    [CommandOption("-d|--debug")]
    [Description("DEPRECATED: Use '--log debug' instead")]
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    [Obsolete("Use LogLevel instead")]
    public bool Debug
    {
        get;
        init
        {
            field = value;
            if (value)
            {
                LogLevel = new FlagValue<CliLogLevel> { IsSet = true, Value = CliLogLevel.Debug };
            }
        }
    }

    [CommandOption("--app-data")]
    [Description("Custom path to the application data directory")]
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public string? AppData { get; init; }

    [CommandOption("--raw")]
    [Description(
        "Omit any boilerplate text or colored formatting. This option primarily exists for scripts."
    )]
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public bool? Raw { get; init; }
}
