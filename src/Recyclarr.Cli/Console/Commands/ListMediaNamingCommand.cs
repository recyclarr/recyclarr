using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Logging;
using Recyclarr.Cli.Pipelines.MediaNaming;
using Recyclarr.Cli.Processors;
using Recyclarr.TrashGuide;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("List media naming formats in the guide for a particular service.")]
internal class ListMediaNamingCommand(
    MediaNamingDataLister lister,
    ConsoleMultiRepoUpdater repoUpdater,
    RecyclarrConsoleSettings consoleSettings
) : AsyncCommand<ListMediaNamingCommand.CliSettings>
{
    [UsedImplicitly]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    internal class CliSettings : BaseCommandSettings
    {
        [CommandArgument(0, "<service_type>")]
        [EnumDescription<SupportedServices>("The service type to obtain information about.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public SupportedServices Service { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CliSettings settings)
    {
        var outputSettings = consoleSettings.GetOutputSettings(settings);
        await repoUpdater.UpdateAllRepositories(outputSettings, settings.CancellationToken);
        lister.ListNaming(settings.Service);
        return (int)ExitStatus.Succeeded;
    }
}
