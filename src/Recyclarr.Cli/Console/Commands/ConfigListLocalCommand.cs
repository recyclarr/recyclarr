using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Processors;
using Recyclarr.Cli.Processors.Config;
using Recyclarr.Repo;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("List local configuration files.")]
internal class ConfigListLocalCommand(
    ConfigListLocalProcessor processor,
    IMultiRepoUpdater repoUpdater
) : AsyncCommand<ConfigListLocalCommand.CliSettings>
{
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    internal class CliSettings : BaseCommandSettings;

    public override async Task<int> ExecuteAsync(CommandContext context, CliSettings settings)
    {
        await repoUpdater.UpdateAllRepositories(settings.Raw, settings.CancellationToken);
        processor.Process();
        return (int)ExitStatus.Succeeded;
    }
}
