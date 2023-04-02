using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Pipelines.CustomFormat.Guide;
using Recyclarr.TrashLib.Repo;
using Spectre.Console.Cli;

#pragma warning disable CS8765

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("List custom formats in the guide for a particular service.")]
internal class ListCustomFormatsCommand : AsyncCommand<ListCustomFormatsCommand.CliSettings>
{
    private readonly CustomFormatDataLister _lister;
    private readonly IRepoUpdater _repoUpdater;

    [UsedImplicitly]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    public class CliSettings : BaseCommandSettings
    {
        [CommandArgument(0, "<service>")]
        [EnumDescription<SupportedServices>("The service to obtain information about.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public SupportedServices Service { get; init; }
    }

    public ListCustomFormatsCommand(
        CustomFormatDataLister lister,
        IRepoUpdater repoUpdater)
    {
        _lister = lister;
        _repoUpdater = repoUpdater;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CliSettings settings)
    {
        await _repoUpdater.UpdateRepo();
        _lister.ListCustomFormats(settings.Service);
        return 0;
    }
}
