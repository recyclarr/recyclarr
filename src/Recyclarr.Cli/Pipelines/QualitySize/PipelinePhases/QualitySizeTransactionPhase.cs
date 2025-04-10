using System.Collections.ObjectModel;
using Recyclarr.ServarrApi.QualityDefinition;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

internal class QualitySizeTransactionPhase(ILogger log) : IPipelinePhase<QualitySizePipelineContext>
{
    public Task<PipelineFlow> Execute(QualitySizePipelineContext context, CancellationToken ct)
    {
        // Do not check ConfigOutput for null since the Config Phase does it for us
        var guideQuality = context.Qualities;
        var serverQuality = context.ApiFetchOutput;

        var newQuality = new Collection<ServiceQualityDefinitionItem>();
        foreach (var qualityData in guideQuality)
        {
            var serverEntry = serverQuality.FirstOrDefault(q =>
                q.Quality?.Name == qualityData.Item.Quality
            );
            if (serverEntry == null)
            {
                log.Warning(
                    "Server lacks quality definition for {Quality}; it will be skipped",
                    qualityData.Item.Quality
                );
                continue;
            }

            var isDifferent = QualityIsDifferent(serverEntry, qualityData);

            log.Debug(
                "Processed Quality {Name}: "
                    + "[IsDifferent: {IsDifferent}] "
                    + "[Min: {Min1}, {Min2}] "
                    + "[Max: {Max1}, {Max2} ({MaxLimit})] "
                    + "[Preferred: {Preferred1}, {Preferred2} ({PreferredLimit})]",
                serverEntry.Quality?.Name,
                isDifferent,
                serverEntry.MinSize,
                qualityData.Item.Min,
                serverEntry.MaxSize,
                qualityData.Item.Max,
                qualityData.Limits.MaxLimit,
                serverEntry.PreferredSize,
                qualityData.Item.Preferred,
                qualityData.Limits.PreferredLimit
            );

            if (!isDifferent)
            {
                continue;
            }

            // Not using the original list again, so it's OK to modify the definition ref type objects in-place.
            serverEntry.MinSize = qualityData.MinForApi;
            serverEntry.MaxSize = qualityData.MaxForApi;
            serverEntry.PreferredSize = qualityData.PreferredForApi;
            newQuality.Add(serverEntry);
        }

        context.TransactionOutput = newQuality;
        return Task.FromResult(PipelineFlow.Continue);
    }

    private static bool QualityIsDifferent(ServiceQualityDefinitionItem a, QualityItemWithLimits b)
    {
        return b.IsMinDifferent(a.MinSize)
            || b.IsMaxDifferent(a.MaxSize)
            || b.IsPreferredDifferent(a.PreferredSize);
    }
}
