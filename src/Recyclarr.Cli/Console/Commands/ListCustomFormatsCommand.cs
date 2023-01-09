using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Autofac.Features.Indexed;
using JetBrains.Annotations;
using Recyclarr.Cli.Console.Commands.Shared;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Repo;
using Recyclarr.TrashLib.Services.Common;
using Spectre.Console.Cli;

#pragma warning disable CS8765

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("List custom formats in the guide for a particular service.")]
internal class ListCustomFormatsCommand : AsyncCommand<ListCustomFormatsCommand.CliSettings>
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

    public ListCustomFormatsCommand(
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
        _lister.ListCustomFormats(guideService.GetCustomFormatData());
        return 0;
    }
}
