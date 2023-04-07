using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Pipelines.Tags.PipelinePhases;
using Recyclarr.TrashLib.Processors;

namespace Recyclarr.TrashLib.Pipelines.Tags;

public interface ITagPipelinePhases
{
    TagConfigPhase ConfigPhase { get; }
    Lazy<TagPreviewPhase> PreviewPhase { get; }
    TagApiFetchPhase ApiFetchPhase { get; }
    TagTransactionPhase TransactionPhase { get; }
    TagApiPersistencePhase ApiPersistencePhase { get; }
}

public class TagSyncPipeline : ISyncPipeline
{
    private readonly ILogger _log;
    private readonly ITagPipelinePhases _phases;

    public TagSyncPipeline(
        ILogger log,
        ITagPipelinePhases phases)
    {
        _log = log;
        _phases = phases;
    }

    public async Task Execute(ISyncSettings settings, IServiceConfiguration config)
    {
        if (config is not SonarrConfiguration sonarrConfig)
        {
            _log.Debug("Skipping tag pipeline because {Instance} is not a Sonarr config", config.InstanceName);
            return;
        }

        var tags = _phases.ConfigPhase.Execute(sonarrConfig);
        if (tags is null)
        {
            _log.Debug("No tags to process");
            return;
        }

        var serviceData = await _phases.ApiFetchPhase.Execute(config);
        var transactions = _phases.TransactionPhase.Execute(tags, serviceData);

        if (settings.Preview)
        {
            _phases.PreviewPhase.Value.Execute(transactions.AsReadOnly());
            return;
        }

        await _phases.ApiPersistencePhase.Execute(config, transactions);
    }
}
