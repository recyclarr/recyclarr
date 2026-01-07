using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

internal class ListCommandSettings : BaseCommandSettings
{
    [CommandOption("--raw")]
    [Description("Output in TSV format for scripting (mutually exclusive with --log)")]
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public bool Raw { get; init; }

    public override ValidationResult Validate()
    {
        return Raw && LogLevel.IsSet
            ? ValidationResult.Error(
                "Cannot use --raw with --log. "
                    + "Use --raw for machine-readable output or --log for diagnostics."
            )
            : ValidationResult.Success();
    }
}
