using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Processors;
using Recyclarr.Cli.Processors.Config;
using Recyclarr.Repo;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("List local configuration files.")]
public class ConfigListLocalCommand(
    ConfigListLocalProcessor processor,
    IMultiRepoUpdater repoUpdater
) : AsyncCommand<ConfigListLocalCommand.CliSettings>
{
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    public class CliSettings : BaseCommandSettings;

    public override async Task<int> ExecuteAsync(CommandContext context, CliSettings settings)
    {
        await repoUpdater.UpdateAllRepositories(settings.CancellationToken);
        processor.Process();
        return (int)ExitStatus.Succeeded;
    }
}
