using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Autofac.Features.Indexed;
using JetBrains.Annotations;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Repo;
using Recyclarr.TrashLib.Services.Common;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

#pragma warning disable CS8765
[UsedImplicitly]
[Description("List quality definitions in the guide for a particular service.")]
internal class ListQualitiesCommand : AsyncCommand<ListQualitiesCommand.CliSettings>
{
    private readonly IGuideDataLister _lister;
    private readonly IIndex<SupportedServices, IGuideService> _guideService;
    private readonly IRepoUpdater _repoUpdater;

    [UsedImplicitly]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    public class CliSettings : BaseCommandSettings
    {
        [CommandArgument(0, "<service>")]
        [EnumDescription<SupportedServices>("The service to obtain information about.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public required SupportedServices Service { get; init; }
    }

    public ListQualitiesCommand(
        IGuideDataLister lister,
        IIndex<SupportedServices, IGuideService> guideService,
        IRepoUpdater repoUpdater)
    {
        _lister = lister;
        _guideService = guideService;
        _repoUpdater = repoUpdater;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CliSettings settings)
    {
        await _repoUpdater.UpdateRepo();
        var guideService = _guideService[settings.Service];
        _lister.ListQualities(guideService.GetQualities());
        return 0;
    }
}
