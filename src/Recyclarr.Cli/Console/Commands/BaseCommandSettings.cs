using System.ComponentModel;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

internal class BaseCommandSettings : CommandSettings
{
    [CommandOption("-d|--debug")]
    [Description("Show debug logs in console output.")]
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public bool Debug { get; init; }

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
