using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using JetBrains.Annotations;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Migration;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Repo;
using Recyclarr.TrashLib.Services.CustomFormat.Guide;
using Recyclarr.TrashLib.Services.Processors;
using Recyclarr.TrashLib.Services.QualitySize.Guide;
using Recyclarr.TrashLib.Services.ReleaseProfile.Guide;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("OBSOLETE: Use `sync sonarr` instead")]
internal class SonarrCommand : AsyncCommand<SonarrCommand.CliSettings>
{
    private readonly ILogger _log;
    private readonly CustomFormatDataLister _cfLister;
    private readonly QualitySizeDataLister _qualityLister;
    private readonly ReleaseProfileDataLister _rpLister;
    private readonly IMigrationExecutor _migration;
    private readonly IRepoUpdater _repoUpdater;
    private readonly ISyncProcessor _syncProcessor;

    [UsedImplicitly]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    public class CliSettings : ServiceCommandSettings, ISyncSettings
    {
        public SupportedServices? Service => SupportedServices.Sonarr;
        public IReadOnlyCollection<string> Instances { get; } = Array.Empty<string>();

        [CommandOption("-p|--preview")]
        [Description("Only display the processed markdown results without making any API calls.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public bool Preview { get; init; }

        [CommandOption("-c|--config")]
        [Description(
            "One or more YAML config files to use. All configs will be used and settings are additive. " +
            "If not specified, the script will look for `recyclarr.yml` in the same directory as the executable.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        [TypeConverter(typeof(FileInfoConverter))]
        public IFileInfo[] ConfigsOption { get; init; } = Array.Empty<IFileInfo>();
        public IReadOnlyCollection<IFileInfo> Configs => ConfigsOption;

        [CommandOption("--list-custom-formats")]
        [Description("List available custom formats from the guide in YAML format.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public bool ListCustomFormats { get; init; }

        [CommandOption("--list-qualities")]
        [Description("List available quality definition types from the guide.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public bool ListQualities { get; init; }

        [CommandOption("--list-release-profiles")]
        [Description("List available release profiles from the guide in YAML format.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public bool ListReleaseProfiles { get; init; }

        // The default value is "empty" because I need to know when the user specifies the option but no value with it.
        // Discussed here: https://github.com/Tyrrrz/CliFx/discussions/128#discussioncomment-2647015
        [CommandOption("--list-terms")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        [Description(
            "For the given Release Profile Trash ID, list terms in it that can be filtered in YAML format. " +
            "Note that not every release profile has terms that may be filtered.")]
        public string? ListTerms { get; init; }
    }

    public SonarrCommand(
        ILogger log,
        CustomFormatDataLister cfLister,
        QualitySizeDataLister qualityLister,
        ReleaseProfileDataLister rpLister,
        IMigrationExecutor migration,
        IRepoUpdater repoUpdater,
        ISyncProcessor syncProcessor)
    {
        _log = log;
        _cfLister = cfLister;
        _qualityLister = qualityLister;
        _rpLister = rpLister;
        _migration = migration;
        _repoUpdater = repoUpdater;
        _syncProcessor = syncProcessor;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CliSettings settings)
    {
        // Will throw if migration is required, otherwise just a warning is issued.
        _migration.CheckNeededMigrations();
        await _repoUpdater.UpdateRepo();

        if (settings.ListCustomFormats)
        {
            _log.Warning("The `sonarr` subcommand is DEPRECATED -- Use `list custom-formats sonarr` instead!");
            _cfLister.ListCustomFormats(SupportedServices.Sonarr);
            return 0;
        }

        if (settings.ListQualities)
        {
            _log.Warning("The `sonarr` subcommand is DEPRECATED -- Use `list qualities sonarr` instead!");
            _qualityLister.ListQualities(SupportedServices.Sonarr);
            return 0;
        }

        if (settings.ListReleaseProfiles)
        {
            _log.Warning("The `sonarr` subcommand is DEPRECATED -- Use `list release-profiles` instead!");
            _rpLister.ListReleaseProfiles();
            return 0;
        }

        if (settings.ListTerms is not null)
        {
            _rpLister.ListTerms(settings.ListTerms);
            return 0;
        }

        _log.Warning("The `sonarr` subcommand is DEPRECATED -- Use `sync` instead!");
        return (int) await _syncProcessor.ProcessConfigs(settings);
    }
}
