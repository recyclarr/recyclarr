using System.Collections.Generic;
using System.Threading.Tasks;
using Trash.Radarr.CustomFormat.Models;
using Trash.Radarr.CustomFormat.Models.Cache;
using Trash.Radarr.CustomFormat.Processors.PersistenceSteps;

namespace Trash.Radarr.CustomFormat.Processors
{
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
}
