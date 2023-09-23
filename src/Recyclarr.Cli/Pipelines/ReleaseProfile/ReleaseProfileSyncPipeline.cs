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

public class ReleaseProfileSyncPipeline : ISyncPipeline
{
    private readonly ILogger _log;
    private readonly IReleaseProfilePipelinePhases _phases;

    public ReleaseProfileSyncPipeline(ILogger log, IReleaseProfilePipelinePhases phases)
    {
        _log = log;
        _phases = phases;
    }

    public async Task Execute(ISyncSettings settings, IServiceConfiguration config)
    {
        if (config is not SonarrConfiguration sonarrConfig)
        {
            _log.Debug("Skipping release profile pipeline because {Instance} is not a Sonarr config",
                config.InstanceName);
            return;
        }

        var profiles = _phases.ConfigPhase.Execute(sonarrConfig);
        if (profiles is null)
        {
            _log.Debug("No release profiles to process");
            return;
        }

        var serviceData = await _phases.ApiFetchPhase.Execute(config);
        var transactions = _phases.TransactionPhase.Execute(profiles, serviceData);

        if (settings.Preview)
        {
            _phases.PreviewPhase.Value.Execute(transactions);
            return;
        }

        await _phases.ApiPersistencePhase.Execute(config, transactions);
    }
}
