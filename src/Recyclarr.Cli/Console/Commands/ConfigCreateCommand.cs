using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Config;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("Create a starter configuration file.")]
internal class ConfigCreateCommand(
    ILogger log,
    IAnsiConsole console,
    IConfigFileCreator creator,
    ProviderProgressHandler providerProgressHandler
) : AsyncCommand<ConfigCreateCommand.CliSettings>
{
    [UsedImplicitly]
    [SuppressMessage(
        "Performance",
        "CA1819:Properties should not return arrays",
        Justification = "Spectre.Console requires it"
    )]
    internal class CliSettings : BaseCommandSettings, ICreateConfigSettings
    {
        [CommandOption("-p|--path")]
        [Description("Path to where the configuration file should be created.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string? Path { get; init; }

        [CommandOption("-t|--template")]
        [Description(
            "One or more template configuration files to create. Use `config list templates` to get a list of "
                + "names accepted here."
        )]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string[] TemplatesOption { get; init; } = [];
        public IReadOnlyCollection<string> Templates => TemplatesOption;

        [CommandOption("-f|--force")]
        [Description("Replace any existing configuration file, if present.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public bool Force { get; init; }
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        CliSettings settings,
        CancellationToken ct
    )
    {
        try
        {
            await providerProgressHandler.InitializeProvidersAsync(silent: false, ct);
            var createdFiles = creator.Create(settings);

            foreach (var file in createdFiles)
            {
                if (file.Replaced)
                {
                    console.MarkupLineInterpolated($"[yellow]Replaced:[/] {file.Path}");
                }
                else
                {
                    console.MarkupLineInterpolated($"[green]Created:[/] {file.Path}");
                }
            }

            return (int)ExitStatus.Succeeded;
        }
        catch (FileExistsException e)
        {
            log.Error(
                e,
                "The file {ConfigFile} already exists. Please choose another path or "
                    + "delete/move the existing file and run this command again",
                e.AttemptedPath
            );
            console.MarkupLineInterpolated(
                $"[red]Error:[/] File already exists: {e.AttemptedPath}"
            );
            console.MarkupLine("[dim]Use -f/--force to replace, or choose a different path.[/]");
        }

        return (int)ExitStatus.Failed;
    }
}
