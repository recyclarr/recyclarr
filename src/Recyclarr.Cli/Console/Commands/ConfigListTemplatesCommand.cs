using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Logging;
using Recyclarr.Cli.Processors;
using Recyclarr.Cli.Processors.Config;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("List local configuration files.")]
internal class ConfigListTemplatesCommand(
    ConfigListTemplateProcessor processor,
    ConsoleGitRepositoryInitializer gitRepositoryInitializer,
    RecyclarrConsoleSettings consoleSettings
) : AsyncCommand<ConfigListTemplatesCommand.CliSettings>
{
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    internal class CliSettings : BaseCommandSettings, IConfigListTemplatesSettings
    {
        [CommandOption("-i|--includes")]
        [Description(
            "List templates that may be included in YAML, instead of root templates used with `config create`."
        )]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public bool Includes { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CliSettings settings)
    {
        var outputSettings = consoleSettings.GetOutputSettings(settings);
        await gitRepositoryInitializer.InitializeGitRepositories(
            outputSettings,
            settings.CancellationToken
        );
        processor.Process(settings);
        return (int)ExitStatus.Succeeded;
    }
}
