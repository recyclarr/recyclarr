using System.Collections.ObjectModel;
using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.ServarrApi.QualityDefinition;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

public class QualitySizeTransactionPhase(ILogger log) : ITransactionPipelinePhase<QualitySizePipelineContext>
{
    public void Execute(QualitySizePipelineContext context)
    {
        // Do not check ConfigOutput for null since the LogPhase does it for us
        var guideQuality = context.ConfigOutput!.Qualities;
        var serverQuality = context.ApiFetchOutput;

        var newQuality = new Collection<ServiceQualityDefinitionItem>();
        foreach (var qualityData in guideQuality)
        {
            var serverEntry = serverQuality.FirstOrDefault(q => q.Quality?.Name == qualityData.Quality);
            if (serverEntry == null)
            {
                log.Warning("Server lacks quality definition for {Quality}; it will be skipped", qualityData.Quality);
                continue;
            }

            var isDifferent = QualityIsDifferent(serverEntry, qualityData);

            log.Debug("Processed Quality {Name}: " +
                "[IsDifferent: {IsDifferent}] " +
                "[Min: {Min1},{Min2}] " +
                "[Max: {Max1},{Max2}] " +
                "[Preferred: {Preferred1},{Preferred2}]",
                serverEntry.Quality?.Name,
                isDifferent,
                serverEntry.MinSize, qualityData.Min,
                serverEntry.MaxSize, qualityData.Max,
                serverEntry.PreferredSize, qualityData.Preferred);

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
    }

    private static bool QualityIsDifferent(ServiceQualityDefinitionItem a, QualityItemWithPreferred b)
    {
        return b.IsMinDifferent(a.MinSize) || b.IsMaxDifferent(a.MaxSize) || b.IsPreferredDifferent(a.PreferredSize);
    }
}
