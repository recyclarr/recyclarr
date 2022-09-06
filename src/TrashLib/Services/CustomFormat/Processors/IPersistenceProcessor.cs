using TrashLib.Services.CustomFormat.Models;
using TrashLib.Services.CustomFormat.Models.Cache;
using TrashLib.Services.CustomFormat.Processors.PersistenceSteps;

namespace TrashLib.Services.CustomFormat.Processors;

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
