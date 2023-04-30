using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Recyclarr.Cli.Processors;
using Recyclarr.TrashLib.ExceptionTypes;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("Create a starter configuration file.")]
public class ConfigCreateCommand : AsyncCommand<ConfigCreateCommand.CliSettings>
{
    private readonly IConfigCreationProcessor _processor;
    private readonly ILogger _log;

    [UsedImplicitly]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    public class CliSettings : BaseCommandSettings
    {
        [CommandOption("-p|--path")]
        [Description("Path to where the configuration file should be created.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string? Path { get; init; }
    }

    public ConfigCreateCommand(ILogger log, IConfigCreationProcessor processor)
    {
        _processor = processor;
        _log = log;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CliSettings settings)
    {
        try
        {
            await _processor.Process(settings.Path);
        }
        catch (FileExistsException e)
        {
            _log.Error(
                "The file {ConfigFile} already exists. Please choose another path or " +
                "delete/move the existing file and run this command again", e.AttemptedPath);

            return 1;
        }

        return 0;
    }
}
