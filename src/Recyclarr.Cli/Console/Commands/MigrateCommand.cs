using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Processors;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("Perform migration steps that may be needed between versions")]
internal class MigrateCommand(IAnsiConsole console, ILogger log)
    : Command<MigrateCommand.CliSettings>
{
    [UsedImplicitly]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    internal class CliSettings : BaseCommandSettings;

    public override int Execute(CommandContext context, CliSettings settings, CancellationToken ct)
    {
        const string message =
            "The migrate command is deprecated. " + "Migrations now run automatically at startup.";

        console.MarkupLine($"[yellow][[DEPRECATED]][/] {message}");
        log.Warning(message);
        return (int)ExitStatus.Succeeded;
    }
}
