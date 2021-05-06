using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Trash.Radarr.CustomFormat.Models;
using Trash.Radarr.CustomFormat.Models.Cache;

namespace Trash.Radarr.CustomFormat.Processors.PersistenceSteps
{
    public interface IJsonTransactionStep
    {
        CustomFormatTransactionData Transactions { get; }

        void Process(IEnumerable<ProcessedCustomFormatData> guideCfs,
            IReadOnlyCollection<JObject> radarrCfs);

        void RecordDeletions(IEnumerable<TrashIdMapping> deletedCfsInCache, List<JObject> radarrCfs);
    }
}
