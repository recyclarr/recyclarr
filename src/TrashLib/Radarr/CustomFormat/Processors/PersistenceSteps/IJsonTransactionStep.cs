using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat.Api.Models;
using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Radarr.CustomFormat.Models.Cache;

namespace TrashLib.Radarr.CustomFormat.Processors.PersistenceSteps
{
    public interface IJsonTransactionStep
    {
        CustomFormatTransactionData Transactions { get; }

        void Process(
            IEnumerable<ProcessedCustomFormatData> guideCfs,
            IReadOnlyCollection<CustomFormatData> radarrCfs,
            RadarrConfig config);

        void RecordDeletions(IEnumerable<ProcessedCustomFormatData> guideCfs,
            List<CustomFormatData> radarrCfs, RadarrConfig config);
    }
}
