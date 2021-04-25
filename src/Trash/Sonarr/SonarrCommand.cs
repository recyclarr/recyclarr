using System;
using System.IO;
using System.Threading.Tasks;
using CliFx.Attributes;
using Flurl.Http;
using JetBrains.Annotations;
using Serilog;
using Serilog.Core;
using Trash.Command;
using Trash.Config;
using Trash.Sonarr.QualityDefinition;
using Trash.Sonarr.ReleaseProfile;

namespace Trash.Sonarr
{
    [Command("sonarr", Description = "Perform operations on a Sonarr instance")]
    [UsedImplicitly]
    public class SonarrCommand : ServiceCommand, ISonarrCommand
    {
        private readonly IConfigurationLoader<SonarrConfiguration> _configLoader;
        private readonly Func<ReleaseProfileUpdater> _profileUpdaterFactory;
        private readonly Func<SonarrQualityDefinitionUpdater> _qualityUpdaterFactory;

        public SonarrCommand(
            ILogger logger,
            LoggingLevelSwitch loggingLevelSwitch,
            IConfigurationLoader<SonarrConfiguration> configLoader,
            Func<ReleaseProfileUpdater> profileUpdaterFactory,
            Func<SonarrQualityDefinitionUpdater> qualityUpdaterFactory)
            : base(logger, loggingLevelSwitch)
        {
            _configLoader = configLoader;
            _profileUpdaterFactory = profileUpdaterFactory;
            _qualityUpdaterFactory = qualityUpdaterFactory;
        }

        // todo: Add options to exclude parts of YAML on the fly?

        public override string CacheStoragePath { get; } =
            Path.Join(AppPaths.AppDataPath, "cache/sonarr");

        public override async Task Process()
        {
            try
            {
                foreach (var config in _configLoader.LoadMany(Config, "sonarr"))
                {
                    if (config.ReleaseProfiles.Count > 0)
                    {
                        await _profileUpdaterFactory().Process(this, config);
                    }

                    if (config.QualityDefinition.HasValue)
                    {
                        await _qualityUpdaterFactory().Process(this, config);
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
