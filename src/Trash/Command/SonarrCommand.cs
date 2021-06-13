using System;
using System.IO;
using System.Threading.Tasks;
using CliFx.Attributes;
using Flurl.Http;
using JetBrains.Annotations;
using Serilog;
using Serilog.Core;
using Trash.Command.Helpers;
using Trash.Config;
using TrashLib.Sonarr.Config;
using TrashLib.Sonarr.QualityDefinition;
using TrashLib.Sonarr.ReleaseProfile;

namespace Trash.Command
{
    [Command("sonarr", Description = "Perform operations on a Sonarr instance")]
    [UsedImplicitly]
    public class SonarrCommand : ServiceCommand
    {
        private readonly IConfigurationLoader<SonarrConfiguration> _configLoader;
        private readonly Func<IReleaseProfileUpdater> _profileUpdaterFactory;
        private readonly Func<ISonarrQualityDefinitionUpdater> _qualityUpdaterFactory;

        public SonarrCommand(
            ILogger logger,
            LoggingLevelSwitch loggingLevelSwitch,
            ILogJanitor logJanitor,
            IConfigurationLoader<SonarrConfiguration> configLoader,
            Func<IReleaseProfileUpdater> profileUpdaterFactory,
            Func<ISonarrQualityDefinitionUpdater> qualityUpdaterFactory)
            : base(logger, loggingLevelSwitch, logJanitor)
        {
            _configLoader = configLoader;
            _profileUpdaterFactory = profileUpdaterFactory;
            _qualityUpdaterFactory = qualityUpdaterFactory;
        }

        // todo: Add options to exclude parts of YAML on the fly?

        public override string CacheStoragePath { get; } =
            Path.Combine(AppPaths.AppDataPath, "cache", "sonarr");

        public override async Task Process()
        {
            try
            {
                foreach (var config in _configLoader.LoadMany(Config, "sonarr"))
                {
                    if (config.ReleaseProfiles.Count > 0)
                    {
                        await _profileUpdaterFactory().Process(Preview, config);
                    }

                    if (config.QualityDefinition.HasValue)
                    {
                        await _qualityUpdaterFactory().Process(Preview, config);
                    }
                }
            }
            catch (FlurlHttpException e)
            {
                Log.Error(e, "HTTP error while communicating with Sonarr");
                ExitDueToFailure();
            }
        }
    }
}
