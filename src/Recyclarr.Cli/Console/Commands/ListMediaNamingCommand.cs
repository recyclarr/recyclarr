using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Pipelines.MediaNaming;
using Recyclarr.Common;
using Recyclarr.Repo;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("List media naming formats in the guide for a particular service.")]
public class ListMediaNamingCommand : AsyncCommand<ListMediaNamingCommand.CliSettings>
{
    private readonly MediaNamingDataLister _lister;
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

    public ListMediaNamingCommand(MediaNamingDataLister lister, IMultiRepoUpdater repoUpdater)
    {
        _lister = lister;
        _repoUpdater = repoUpdater;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CliSettings settings)
    {
        await _repoUpdater.UpdateAllRepositories(settings.CancellationToken);
        _lister.ListNaming(settings.Service);
        return 0;
    }
}
