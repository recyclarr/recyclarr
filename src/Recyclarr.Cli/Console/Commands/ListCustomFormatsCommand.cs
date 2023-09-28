using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines.CustomFormat;
using Recyclarr.Common;
using Recyclarr.Repo;
using Spectre.Console.Cli;

#pragma warning disable CS8765

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("List custom formats in the guide for a particular service.")]
public class ListCustomFormatsCommand : AsyncCommand<ListCustomFormatsCommand.CliSettings>
{
    private readonly CustomFormatDataLister _lister;
    private readonly IMultiRepoUpdater _repoUpdater;

    [UsedImplicitly]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    public class CliSettings : BaseCommandSettings, IListCustomFormatSettings
    {
        [CommandArgument(0, "<service_type>")]
        [EnumDescription<SupportedServices>("The service type to obtain information about.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public SupportedServices Service { get; init; }

        [CommandOption("--score-sets")]
        [Description("Instead of listing custom formats, list the score sets all custom formats are part of.")]
        public bool ScoreSets { get; init; } = false;

        [CommandOption("--raw")]
        [Description("Omit any boilerplate text or colored formatting. This option primarily exists for scripts.")]
        public bool Raw { get; init; } = false;
    }

    public ListCustomFormatsCommand(CustomFormatDataLister lister, IMultiRepoUpdater repoUpdater)
    {
        _lister = lister;
        _repoUpdater = repoUpdater;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CliSettings settings)
    {
        await _repoUpdater.UpdateAllRepositories(settings.CancellationToken, settings.Raw);
        _lister.List(settings);
        return 0;
    }
}
