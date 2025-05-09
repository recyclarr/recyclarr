using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Logging;
using Recyclarr.Cli.Processors;
using Recyclarr.Cli.Processors.Config;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("Create a starter configuration file.")]
internal class ConfigCreateCommand(
    ILogger log,
    IConfigCreationProcessor processor,
    ConsoleMultiRepoUpdater repoUpdater,
    RecyclarrConsoleSettings consoleSettings
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

    public override async Task<int> ExecuteAsync(CommandContext context, CliSettings settings)
    {
        try
        {
            var outputSettings = consoleSettings.GetOutputSettings(settings);
            await repoUpdater.UpdateAllRepositories(outputSettings, settings.CancellationToken);
            processor.Process(settings);
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
        }

        return (int)ExitStatus.Failed;
    }
}
