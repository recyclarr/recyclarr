using System.ComponentModel;
using JetBrains.Annotations;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

public class BaseCommandSettings : CommandSettings
{
    public CancellationToken CancellationToken { get; set; }

    [CommandOption("-d|--debug")]
    [Description("Show debug logs in console output.")]
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public bool Debug { get; init; }
}
