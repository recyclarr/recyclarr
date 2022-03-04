using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Radarr.CustomFormat.Models.Cache;
using TrashLib.Radarr.CustomFormat.Processors.PersistenceSteps;

namespace TrashLib.Radarr.CustomFormat.Processors;

public interface IPersistenceProcessor
{
    IDictionary<string, List<UpdatedFormatScore>> UpdatedScores { get; }
    IReadOnlyCollection<string> InvalidProfileNames { get; }
    CustomFormatTransactionData Transactions { get; }

    Task PersistCustomFormats(IReadOnlyCollection<ProcessedCustomFormatData> guideCfs,
        IEnumerable<TrashIdMapping> deletedCfsInCache,
        IDictionary<string, QualityProfileCustomFormatScoreMapping> profileScores);

    void Reset();
}
