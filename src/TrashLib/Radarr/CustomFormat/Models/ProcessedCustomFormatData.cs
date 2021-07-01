using Newtonsoft.Json;
using TrashLib.Radarr.CustomFormat.Api.Models;

namespace TrashLib.Radarr.CustomFormat.Models
{
    public class ProcessedCustomFormatData
    {
        public ProcessedCustomFormatData(string trashId, CustomFormatData data)
        {
            TrashId = trashId;
            Data = data;
        }

        public string Name => Data.Name;
        public string TrashId { get; }
        public int? Score { get; init; }

        public CustomFormatData Data { get; }

        public static ProcessedCustomFormatData CreateFromJson(string guideData)
        {
            var cfData = JsonConvert.DeserializeObject<CustomFormatData>(guideData);
            var trashId = (string) cfData.ExtraJson["trash_id"];
            int? finalScore = null;

            if (cfData.ExtraJson.TryGetValue("trash_score", out var score))
            {
                finalScore = (int) score;
                cfData.ExtraJson.Property("trash_score").Remove();
            }

            // Remove trash_id, it's metadata that is not meant for Radarr itself
            // Radarr supposedly drops this anyway, but I prefer it to be removed by TrashUpdater
            cfData.ExtraJson.Property("trash_id").Remove();

            return new ProcessedCustomFormatData(trashId, cfData) {Score = finalScore};
        }
    }
}
