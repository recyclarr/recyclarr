using Newtonsoft.Json.Linq;
using TrashLib.Services.Radarr.CustomFormat.Models;
using TrashLib.Services.Radarr.CustomFormat.Models.Cache;

namespace TrashLib.Services.Radarr.CustomFormat.Processors.PersistenceSteps;

public interface IJsonTransactionStep
{
    CustomFormatTransactionData Transactions { get; }

    void Process(IEnumerable<ProcessedCustomFormatData> guideCfs,
        IReadOnlyCollection<JObject> radarrCfs);

    void RecordDeletions(IEnumerable<TrashIdMapping> deletedCfsInCache, IEnumerable<JObject> radarrCfs);
}
