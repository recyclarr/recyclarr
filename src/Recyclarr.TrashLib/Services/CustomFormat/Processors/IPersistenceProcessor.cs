using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Services.CustomFormat.Models;
using Recyclarr.TrashLib.Services.CustomFormat.Models.Cache;
using Recyclarr.TrashLib.Services.CustomFormat.Processors.PersistenceSteps;

namespace Recyclarr.TrashLib.Services.CustomFormat.Processors;

public interface IPersistenceProcessor
{
    IDictionary<string, List<UpdatedFormatScore>> UpdatedScores { get; }
    IReadOnlyCollection<string> InvalidProfileNames { get; }
    CustomFormatTransactionData Transactions { get; }

    Task PersistCustomFormats(
        IServiceConfiguration config,
        IReadOnlyCollection<ProcessedCustomFormatData> guideCfs,
        IEnumerable<TrashIdMapping> deletedCfsInCache,
        IDictionary<string, QualityProfileCustomFormatScoreMapping> profileScores);
}
