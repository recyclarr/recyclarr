using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Logging;
using Recyclarr.Cli.Processors;
using Recyclarr.Cli.Processors.Config;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("List local configuration files.")]
internal class ConfigListLocalCommand(
    ConfigListLocalProcessor processor,
    ConsoleGitRepositoryInitializer gitRepositoryInitializer,
    RecyclarrConsoleSettings consoleSettings
) : AsyncCommand<ConfigListLocalCommand.CliSettings>
{
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    internal class CliSettings : BaseCommandSettings;

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CliSettings settings,
        CancellationToken ct
    )
    {
        var outputSettings = consoleSettings.GetOutputSettings(settings);
        await gitRepositoryInitializer.InitializeGitRepositories(outputSettings, ct);
        processor.Process();
        return (int)ExitStatus.Succeeded;
    }
}
