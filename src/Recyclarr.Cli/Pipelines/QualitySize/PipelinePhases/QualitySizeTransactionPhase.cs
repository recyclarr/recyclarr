using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.ServarrApi.QualityDefinition;
using Recyclarr.Sync.Events;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

internal class QualitySizeTransactionPhase(ILogger log, ISyncEventCollector eventCollector)
    : IPipelinePhase<QualitySizePipelineContext>
{
    public Task<PipelineFlow> Execute(QualitySizePipelineContext context, CancellationToken ct)
    {
        if (!context.Plan.QualitySizesAvailable)
        {
            return Task.FromResult(PipelineFlow.Terminate);
        }

        var planned = context.Plan.QualitySizes;
        var limits = context.Limits;
        var serverQuality = context.ApiFetchOutput;

        var updatedItems = new List<UpdatedQualityItem>();
        foreach (var plannedQuality in planned.Qualities)
        {
            var serverEntry = serverQuality.FirstOrDefault(q =>
                q.Quality?.Name == plannedQuality.Quality
            );
            if (serverEntry == null)
            {
                eventCollector.AddWarning(
                    $"Server lacks quality definition for {plannedQuality.Quality}; it will be skipped"
                );
                continue;
            }

            var resolved = ResolveValues(plannedQuality, planned.PreferredRatio, limits);
            var item = new UpdatedQualityItem
            {
                Quality = plannedQuality.Quality,
                Min = resolved.Min,
                Max = resolved.Max,
                Preferred = resolved.Preferred,
                IsDifferent = IsDifferent(serverEntry, resolved, limits),
                ServerItem = serverEntry,
            };

            log.Debug(
                "Processed Quality {Name}: "
                    + "[IsDifferent: {IsDifferent}] "
                    + "[Min: {Min1}, {Min2}] "
                    + "[Max: {Max1}, {Max2} ({MaxLimit})] "
                    + "[Preferred: {Preferred1}, {Preferred2} ({PreferredLimit})]",
                item.Quality,
                item.IsDifferent,
                serverEntry.MinSize,
                item.Min,
                serverEntry.MaxSize,
                item.Max,
                limits.MaxLimit,
                serverEntry.PreferredSize,
                item.Preferred,
                limits.PreferredLimit
            );

            updatedItems.Add(item);
        }

        context.TransactionOutput = updatedItems;
        context.QualityDefinitionType = planned.Type;

        return Task.FromResult(PipelineFlow.Continue);
    }

    private static (decimal Min, decimal Max, decimal Preferred) ResolveValues(
        PlannedQualityItem planned,
        decimal? preferredRatio,
        QualityItemLimits limits
    )
    {
        var min = planned.Min;
        var max = Math.Min(planned.Max ?? limits.MaxLimit, limits.MaxLimit);
        var preferred = Math.Min(planned.Preferred ?? limits.PreferredLimit, limits.PreferredLimit);

        if (preferredRatio is not null)
        {
            var cappedMax = Math.Min(max, limits.PreferredLimit);
            preferred = Math.Round(min + (cappedMax - min) * preferredRatio.Value, decimals: 1);
        }

        return (min, max, preferred);
    }

    private static bool IsDifferent(
        ServiceQualityDefinitionItem server,
        (decimal Min, decimal Max, decimal Preferred) resolved,
        QualityItemLimits limits
    )
    {
        if (server.MinSize != resolved.Min)
        {
            return true;
        }

        var serverMax = server.MaxSize ?? limits.MaxLimit;
        if (serverMax != resolved.Max)
        {
            return true;
        }

        var serverPreferred = server.PreferredSize ?? limits.PreferredLimit;
        return serverPreferred != resolved.Preferred;
    }
}
