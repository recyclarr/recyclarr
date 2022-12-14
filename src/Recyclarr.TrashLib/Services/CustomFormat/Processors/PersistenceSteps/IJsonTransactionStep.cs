using Newtonsoft.Json.Linq;
using Recyclarr.TrashLib.Services.CustomFormat.Models;
using Recyclarr.TrashLib.Services.CustomFormat.Models.Cache;

namespace Recyclarr.TrashLib.Services.CustomFormat.Processors.PersistenceSteps;

public interface IJsonTransactionStep
{
    CustomFormatTransactionData Transactions { get; }

    void Process(IEnumerable<ProcessedCustomFormatData> guideCfs,
        IReadOnlyCollection<JObject> serviceCfs);

    void RecordDeletions(IEnumerable<TrashIdMapping> deletedCfsInCache, IEnumerable<JObject> serviceCfs);
}
