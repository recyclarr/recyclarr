using Autofac;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using JetBrains.Annotations;
using Recyclarr.Cli.Config;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Http;
using Recyclarr.TrashLib.Services.CustomFormat;
using Recyclarr.TrashLib.Services.QualitySize;
using Recyclarr.TrashLib.Services.Sonarr;
using Recyclarr.TrashLib.Services.Sonarr.Config;
using Recyclarr.TrashLib.Services.Sonarr.ReleaseProfile;
using Recyclarr.TrashLib.Services.Sonarr.ReleaseProfile.Guide;
using Serilog;

namespace Recyclarr.Cli.Command;

[Command("sonarr", Description = "Perform operations on a Sonarr instance")]
[UsedImplicitly]
public class SonarrCommand : ServiceCommand
{
    // ReSharper disable MemberCanBePrivate.Global

    [CommandOption("list-release-profiles", Description =
        "List available release profiles from the guide in YAML format.")]
    public bool ListReleaseProfiles { get; [UsedImplicitly] set; }

    // The default value is "empty" because I need to know when the user specifies the option but no value with it.
    // Discussed here: https://github.com/Tyrrrz/CliFx/discussions/128#discussioncomment-2647015
    [CommandOption("list-terms", Description =
        "For the given Release Profile Trash ID, list terms in it that can be filtered in YAML format. " +
        "Note that not every release profile has terms that may be filtered.")]
    public string? ListTerms { get; [UsedImplicitly] set; } = "empty";

    [CommandOption("list-qualities", Description =
        "List available quality definition types from the guide.")]
    public bool ListQualities { get; [UsedImplicitly] set; }

    [CommandOption("list-custom-formats", Description =
        "List available custom formats from the guide in YAML format.")]
    public bool ListCustomFormats { get; [UsedImplicitly] set; }

    // ReSharper restore MemberCanBePrivate.Global

    public override string Name => "Sonarr";

    public override async Task Process(ILifetimeScope container)
    {
        await base.Process(container);

        var lister = container.Resolve<ISonarrGuideDataLister>();
        var log = container.Resolve<ILogger>();
        var guideService = container.Resolve<ISonarrGuideService>();
        var console = container.Resolve<IConsole>();

        if (ListReleaseProfiles)
        {
            lister.ListReleaseProfiles();
            return;
        }

        if (ListQualities)
        {
            lister.ListQualities();
            return;
        }

        if (ListCustomFormats)
        {
            lister.ListCustomFormats();
            return;
        }

        if (ListTerms != "empty")
        {
            if (!string.IsNullOrEmpty(ListTerms))
            {
                lister.ListTerms(ListTerms);
            }
            else
            {
                throw new CommandException(
                    "The --list-terms option was specified without a Release Profile Trash ID specified");
            }

            return;
        }

        var configFinder = container.Resolve<IConfigurationFinder>();
        var configLoader = container.Resolve<IConfigurationLoader<SonarrConfiguration>>();
        foreach (var config in configLoader.LoadMany(configFinder.GetConfigFiles(Config), "sonarr"))
        {
            await using var scope = container.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(config).As<IServiceConfiguration>();
            });

            var serverName = Name;
            var instanceName = config.Name ?? FlurlLogging.SanitizeUrl(config.BaseUrl);

            await console.Output.WriteLineAsync($@"
===========================================
Processing {serverName} Server: [{instanceName}]
===========================================
");

            log.Debug("Processing {Server} server {Name}", serverName, instanceName);

            var validator = scope.Resolve<ConfigValidationExecutor>();
            if (!validator.Validate(config))
            {
                log.Error("Due to validation failure, this instance will be skipped");
                continue;
            }

            // ReSharper disable InvertIf

            if (config.ReleaseProfiles.Count > 0)
            {
                var updater = scope.Resolve<IReleaseProfileUpdater>();
                await updater.Process(Preview, config);
            }

            if (config.QualityDefinition != null)
            {
                var updater = scope.Resolve<IQualitySizeUpdater>();
                await updater.Process(Preview, config.QualityDefinition, guideService);
            }

            if (config.CustomFormats.Count > 0)
            {
                var updater = scope.Resolve<ICustomFormatUpdater>();
                await updater.Process(Preview, config.CustomFormats, guideService);
            }

            // ReSharper restore InvertIf
        }
    }
}
