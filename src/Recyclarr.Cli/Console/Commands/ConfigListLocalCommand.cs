using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Recyclarr.Cli.Processors.Config;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Repo;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("List local configuration files.")]
public class ConfigListLocalCommand(ILogger log, ConfigListLocalProcessor processor, IMultiRepoUpdater repoUpdater)
    : AsyncCommand<ConfigListLocalCommand.CliSettings>
{
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    public class CliSettings : BaseCommandSettings
    {
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CliSettings settings)
    {
        try
        {
            await repoUpdater.UpdateAllRepositories(settings.CancellationToken);
            processor.Process();
            return 0;
        }
        catch (NoConfigurationFilesException)
        {
            log.Error("No configuration files found");
        }

        return 1;
    }
}
