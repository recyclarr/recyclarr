using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Pipelines.QualitySize;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Repo;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

#pragma warning disable CS8765
[UsedImplicitly]
[Description("List quality definitions in the guide for a particular service.")]
public class ListQualitiesCommand : AsyncCommand<ListQualitiesCommand.CliSettings>
{
    private readonly QualitySizeDataLister _lister;
    private readonly IMultiRepoUpdater _repoUpdater;

    [UsedImplicitly]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    public class CliSettings : BaseCommandSettings
    {
        [CommandArgument(0, "<service_type>")]
        [EnumDescription<SupportedServices>("The service type to obtain information about.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public SupportedServices Service { get; init; }
    }

    public ListQualitiesCommand(QualitySizeDataLister lister, IMultiRepoUpdater repoUpdater)
    {
        _lister = lister;
        _repoUpdater = repoUpdater;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CliSettings settings)
    {
        await _repoUpdater.UpdateAllRepositories(settings.CancellationToken);
        _lister.ListQualities(settings.Service);
        return 0;
    }
}
