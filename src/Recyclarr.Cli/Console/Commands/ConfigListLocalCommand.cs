using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Processors.Config;
using Recyclarr.Config.Parsing.ErrorHandling;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("List local configuration files.")]
public class ConfigListLocalCommand(
    ILogger log,
    ConfigListLocalProcessor processor,
    CliMultiRepoUpdater repoUpdater)
    : AsyncCommand<ConfigListLocalCommand.CliSettings>
{
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    public class CliSettings : BaseCommandSettings;

    public override async Task<int> ExecuteAsync(CommandContext context, CliSettings settings)
    {
        await repoUpdater.UpdateAllRepositories();

        try
        {
            processor.Process();
        }
        catch (NoConfigurationFilesException e)
        {
            log.Error(e, "Unable to list local config files");
            return 1;
        }

        return 0;
    }
}
