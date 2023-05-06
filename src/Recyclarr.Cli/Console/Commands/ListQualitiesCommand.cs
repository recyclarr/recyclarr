using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Pipelines.QualitySize.Guide;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Repo;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

#pragma warning disable CS8765
[UsedImplicitly]
[Description("List quality definitions in the guide for a particular service.")]
internal class ListQualitiesCommand : AsyncCommand<ListQualitiesCommand.CliSettings>
{
    private readonly QualitySizeDataLister _lister;
    private readonly ITrashGuidesRepo _repoUpdater;

    [UsedImplicitly]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    public class CliSettings : BaseCommandSettings
    {
        [CommandArgument(0, "<service>")]
        [EnumDescription<SupportedServices>("The service to obtain information about.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public SupportedServices Service { get; init; }
    }

    public ListQualitiesCommand(QualitySizeDataLister lister, ITrashGuidesRepo repo)
    {
        _lister = lister;
        _repoUpdater = repo;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CliSettings settings)
    {
        await _repoUpdater.Update();
        _lister.ListQualities(settings.Service);
        return 0;
    }
}
