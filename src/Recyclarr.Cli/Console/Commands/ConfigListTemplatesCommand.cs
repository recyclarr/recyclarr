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
public class ConfigListTemplatesCommand(
    ILogger log,
    ConfigListTemplateProcessor processor,
    CliMultiRepoUpdater repoUpdater)
    : AsyncCommand<ConfigListTemplatesCommand.CliSettings>
{
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    public class CliSettings : BaseCommandSettings, IConfigListTemplatesSettings
    {
        [CommandOption("-i|--includes")]
        [Description(
            "List templates that may be included in YAML, instead of root templates used with `config create`.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public bool Includes { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CliSettings settings)
    {
        await repoUpdater.UpdateAllRepositories();

        try
        {
            processor.Process(settings);
        }
        catch (NoConfigurationFilesException e)
        {
            log.Error(e, "Unable to list template files");
            return 1;
        }

        return 0;
    }
}

public interface IConfigListTemplatesSettings
{
    bool Includes { get; }
}
