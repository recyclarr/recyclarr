using System.Collections.ObjectModel;
using Recyclarr.Cli.Cache;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;
using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Cli.Pipelines.CustomFormat;

public interface ICustomFormatPipelinePhases
{
    CustomFormatConfigPhase ConfigPhase { get; }
    CustomFormatApiFetchPhase ApiFetchPhase { get; }
    CustomFormatTransactionPhase TransactionPhase { get; }
    CustomFormatPreviewPhase PreviewPhase { get; }
    CustomFormatApiPersistencePhase ApiPersistencePhase { get; }
}

public record CustomFormatTransactionData
{
    public Collection<TrashIdMapping> DeletedCustomFormats { get; } = new();
    public Collection<CustomFormatData> NewCustomFormats { get; } = new();
    public Collection<CustomFormatData> UpdatedCustomFormats { get; } = new();
    public Collection<ConflictingCustomFormat> ConflictingCustomFormats { get; } = new();
    public Collection<CustomFormatData> UnchangedCustomFormats { get; } = new();
}

public class CustomFormatSyncPipeline(
    ILogger log,
    ICachePersister cachePersister,
    ICustomFormatPipelinePhases phases)
    : ISyncPipeline
{
    public async Task Execute(ISyncSettings settings, IServiceConfiguration config)
    {
        var cache = cachePersister.Load(config);

        var guideCfs = phases.ConfigPhase.Execute(config);
        if (guideCfs.IsEmpty())
        {
            log.Debug("No custom formats to process");
            return;
        }

        var serviceData = await phases.ApiFetchPhase.Execute(config);

        cache = cache.RemoveStale(serviceData);

        var transactions = phases.TransactionPhase.Execute(config, guideCfs, serviceData, cache);

        phases.PreviewPhase.Execute(transactions);

        if (settings.Preview)
        {
            return;
        }

        await phases.ApiPersistencePhase.Execute(config, transactions);

        cachePersister.Save(config, cache.Update(transactions) with
        {
            InstanceName = config.InstanceName
        });
    }
}
