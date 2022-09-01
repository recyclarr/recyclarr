using Newtonsoft.Json.Linq;
using TrashLib.Services.CustomFormat.Models;
using TrashLib.Services.CustomFormat.Models.Cache;

namespace TrashLib.Services.CustomFormat.Processors.PersistenceSteps;

public interface IJsonTransactionStep
{
    CustomFormatTransactionData Transactions { get; }

    void Process(IEnumerable<ProcessedCustomFormatData> guideCfs,
        IReadOnlyCollection<JObject> radarrCfs);

    void RecordDeletions(IEnumerable<TrashIdMapping> deletedCfsInCache, IEnumerable<JObject> radarrCfs);
}
