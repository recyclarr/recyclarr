using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Processors;
using Recyclarr.Cli.Processors.Config;
using Recyclarr.Repo;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("List local configuration files.")]
public class ConfigListTemplatesCommand(ConfigListTemplateProcessor processor, IMultiRepoUpdater repoUpdater)
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
        await repoUpdater.UpdateAllRepositories(settings.CancellationToken);
        processor.Process(settings);
        return (int) ExitStatus.Succeeded;
    }
}

public interface IConfigListTemplatesSettings
{
    bool Includes { get; }
}
