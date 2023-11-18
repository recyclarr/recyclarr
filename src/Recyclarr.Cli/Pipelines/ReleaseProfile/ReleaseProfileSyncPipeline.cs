using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines.ReleaseProfile.PipelinePhases;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile;

public interface IReleaseProfilePipelinePhases
{
    ReleaseProfileConfigPhase ConfigPhase { get; }
    ReleaseProfileApiFetchPhase ApiFetchPhase { get; }
    ReleaseProfileTransactionPhase TransactionPhase { get; }
    Lazy<ReleaseProfilePreviewPhase> PreviewPhase { get; }
    ReleaseProfileApiPersistencePhase ApiPersistencePhase { get; }
}

public class ReleaseProfileSyncPipeline(ILogger log, IReleaseProfilePipelinePhases phases) : ISyncPipeline
{
    public async Task Execute(ISyncSettings settings, IServiceConfiguration config)
    {
        if (config is not SonarrConfiguration sonarrConfig)
        {
            log.Debug("Skipping release profile pipeline because {Instance} is not a Sonarr config",
                config.InstanceName);
            return;
        }

        var profiles = phases.ConfigPhase.Execute(sonarrConfig);
        if (profiles is null)
        {
            log.Debug("No release profiles to process");
            return;
        }

        var serviceData = await phases.ApiFetchPhase.Execute(config);
        var transactions = phases.TransactionPhase.Execute(profiles, serviceData);

        if (settings.Preview)
        {
            phases.PreviewPhase.Value.Execute(transactions);
            return;
        }

        await phases.ApiPersistencePhase.Execute(config, transactions);
    }
}
