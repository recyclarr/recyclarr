using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Processors.Config;
using Recyclarr.TrashLib.Config.Listers;
using Recyclarr.TrashLib.Config.Parsing.ErrorHandling;
using Recyclarr.TrashLib.ExceptionTypes;
using Recyclarr.TrashLib.Repo;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("List configuration files in various ways.")]
public class ConfigListCommand : AsyncCommand<ConfigListCommand.CliSettings>
{
    private readonly ILogger _log;
    private readonly ConfigListProcessor _processor;
    private readonly IMultiRepoUpdater _repoUpdater;

    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    public class CliSettings : BaseCommandSettings
    {
        [CommandArgument(0, "[ListCategory]")]
        [EnumDescription<ConfigCategory>(
            "The type of configuration information to list. If not specified, defaults to 'local'.")]
        public ConfigCategory ListCategory { get; [UsedImplicitly] init; } = ConfigCategory.Local;
    }

    public ConfigListCommand(ILogger log, ConfigListProcessor processor, IMultiRepoUpdater repoUpdater)
    {
        _log = log;
        _processor = processor;
        _repoUpdater = repoUpdater;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CliSettings settings)
    {
        try
        {
            await _repoUpdater.UpdateAllRepositories(settings.CancellationToken);
            _processor.Process(settings.ListCategory);
        }
        catch (FileExistsException e)
        {
            _log.Error(
                "The file {ConfigFile} already exists. Please choose another path or " +
                "delete/move the existing file and run this command again", e.AttemptedPath);

            return 1;
        }
        catch (NoConfigurationFilesException)
        {
            _log.Error("No configuration files found");
        }

        return 0;
    }
}
