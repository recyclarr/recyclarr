using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines.Tags.PipelinePhases;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Pipelines.Tags;

public interface ITagPipelinePhases
{
    TagConfigPhase ConfigPhase { get; }
    Lazy<TagPreviewPhase> PreviewPhase { get; }
    TagApiFetchPhase ApiFetchPhase { get; }
    TagTransactionPhase TransactionPhase { get; }
    TagApiPersistencePhase ApiPersistencePhase { get; }
}

public class TagSyncPipeline(
    ILogger log,
    ITagPipelinePhases phases) : ISyncPipeline
{
    public async Task Execute(ISyncSettings settings, IServiceConfiguration config)
    {
        if (config is not SonarrConfiguration sonarrConfig)
        {
            log.Debug("Skipping tag pipeline because {Instance} is not a Sonarr config", config.InstanceName);
            return;
        }

        var tags = phases.ConfigPhase.Execute(sonarrConfig);
        if (tags is null)
        {
            log.Debug("No tags to process");
            return;
        }

        var serviceData = await phases.ApiFetchPhase.Execute(config);
        var transactions = phases.TransactionPhase.Execute(tags, serviceData);

        if (settings.Preview)
        {
            phases.PreviewPhase.Value.Execute(transactions.AsReadOnly());
            return;
        }

        await phases.ApiPersistencePhase.Execute(config, transactions);
    }
}
