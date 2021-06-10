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
using Trash.Radarr.CustomFormat;
using Trash.Radarr.QualityDefinition;

namespace Trash.Radarr
{
    [Command("radarr", Description = "Perform operations on a Radarr instance")]
    [UsedImplicitly]
    public class RadarrCommand : ServiceCommand, IRadarrCommand
    {
        private readonly IConfigurationLoader<RadarrConfiguration> _configLoader;
        private readonly Func<ICustomFormatUpdater> _customFormatUpdaterFactory;
        private readonly Func<RadarrQualityDefinitionUpdater> _qualityUpdaterFactory;

        public RadarrCommand(
            ILogger logger,
            LoggingLevelSwitch loggingLevelSwitch,
            ILogJanitor logJanitor,
            IConfigurationLoader<RadarrConfiguration> configLoader,
            Func<RadarrQualityDefinitionUpdater> qualityUpdaterFactory,
            Func<ICustomFormatUpdater> customFormatUpdaterFactory)
            : base(logger, loggingLevelSwitch, logJanitor)
        {
            _configLoader = configLoader;
            _qualityUpdaterFactory = qualityUpdaterFactory;
            _customFormatUpdaterFactory = customFormatUpdaterFactory;
        }

        public override string CacheStoragePath { get; } =
            Path.Combine(AppPaths.AppDataPath, "cache", "radarr");

        public override async Task Process()
        {
            try
            {
                foreach (var config in _configLoader.LoadMany(Config, "radarr"))
                {
                    if (config.QualityDefinition != null)
                    {
                        await _qualityUpdaterFactory().Process(this, config);
                    }

                    if (config.CustomFormats.Count > 0)
                    {
                        await _customFormatUpdaterFactory().Process(this, config);
                    }
                }
            }
            catch (FlurlHttpException e)
            {
                Log.Error(e, "HTTP error while communicating with Radarr");
                ExitDueToFailure();
            }
        }
    }
}
