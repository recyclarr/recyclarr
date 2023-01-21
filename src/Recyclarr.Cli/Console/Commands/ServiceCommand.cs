using System.ComponentModel;
using JetBrains.Annotations;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

public class ServiceCommandSettings : BaseCommandSettings
{
    [CommandOption("--app-data")]
    [Description("Custom path to the application data directory")]
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public string? AppData { get; init; }
}
