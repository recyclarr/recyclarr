using System;
using System.Threading.Tasks;
using CliFx.Attributes;
using CliFx.Exceptions;
using Flurl.Http;
using JetBrains.Annotations;
using Serilog;
using Trash.Command;
using Trash.Config;
using Trash.Sonarr.QualityDefinition;
using Trash.Sonarr.ReleaseProfile;
using YamlDotNet.Core;

namespace Trash.Sonarr
{
    [Command("sonarr", Description = "Perform operations on a Sonarr instance")]
    [UsedImplicitly]
    public class SonarrCommand : BaseCommand, ISonarrCommand
    {
        private readonly IConfigurationLoader<SonarrConfiguration> _configLoader;
        private readonly Func<ReleaseProfileUpdater> _profileUpdaterFactory;
        private readonly Func<SonarrQualityDefinitionUpdater> _qualityUpdaterFactory;

        public SonarrCommand(
            IConfigurationLoader<SonarrConfiguration> configLoader,
            Func<ReleaseProfileUpdater> profileUpdaterFactory,
            Func<SonarrQualityDefinitionUpdater> qualityUpdaterFactory)
        {
            _configLoader = configLoader;
            _profileUpdaterFactory = profileUpdaterFactory;
            _qualityUpdaterFactory = qualityUpdaterFactory;
        }

        // todo: Add options to exclude parts of YAML on the fly?

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
