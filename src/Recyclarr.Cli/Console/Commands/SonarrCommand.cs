using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using JetBrains.Annotations;
using Recyclarr.Cli.Console.Commands.Shared;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Migration;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Services.Processors;
using Recyclarr.TrashLib.Services.Sonarr;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("OBSOLETE: Use `sync sonarr` instead")]
internal class SonarrCommand : AsyncCommand<SonarrCommand.CliSettings>
{
    private readonly ILogger _log;
    private readonly ISonarrGuideDataLister _lister;
    private readonly IMigrationExecutor _migration;
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
        public string? ListTerms { get; init; } = "empty";

        public override ValidationResult Validate()
        {
            if (string.IsNullOrEmpty(ListTerms))
            {
                return ValidationResult.Error(
                    "The --list-terms option was specified without a Release Profile Trash ID specified");
            }

            return base.Validate();
        }
    }

    public SonarrCommand(
        ILogger log,
        ISonarrGuideDataLister lister,
        IMigrationExecutor migration,
        ISyncProcessor syncProcessor)
    {
        _log = log;
        _lister = lister;
        _migration = migration;
        _syncProcessor = syncProcessor;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CliSettings settings)
    {
        _log.Warning("The `sonarr` subcommand is DEPRECATED -- Use `sync` instead!");

        if (settings.ListCustomFormats)
        {
            _lister.ListCustomFormats();
            return 0;
        }

        if (settings.ListQualities)
        {
            _lister.ListQualities();
            return 0;
        }

        if (settings.ListReleaseProfiles)
        {
            _lister.ListReleaseProfiles();
            return 0;
        }

        if (settings.ListTerms != "empty")
        {
            // Ignore nullability of ListTerms since the Settings.Validate() method will check for null/empty.
            _lister.ListTerms(settings.ListTerms!);
            return 0;
        }

        // Will throw if migration is required, otherwise just a warning is issued.
        _migration.CheckNeededMigrations();

        return (int) await _syncProcessor.ProcessConfigs(settings);
    }
}
