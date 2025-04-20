using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Logging;
using Recyclarr.Cli.Pipelines.QualitySize;
using Recyclarr.Cli.Processors;
using Recyclarr.TrashGuide;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

#pragma warning disable CS8765
[UsedImplicitly]
[Description("List quality definitions in the guide for a particular service.")]
internal class ListQualitiesCommand(
    QualitySizeDataLister lister,
    ConsoleMultiRepoUpdater repoUpdater,
    RecyclarrConsoleSettings consoleSettings
) : AsyncCommand<ListQualitiesCommand.CliSettings>
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
        lister.ListQualities(settings.Service);
        return (int)ExitStatus.Succeeded;
    }
}
