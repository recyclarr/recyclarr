using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TrashLib.Radarr.CustomFormat.Models;

namespace TrashLib.Radarr.CustomFormat.Api
{
    public class RadarrCustomFormatData
    {
        public int Id { get; set; }
        public string Name { get; set; }

        [JsonExtensionData, UsedImplicitly]
        private JObject? _extraJson;
    }

    public interface ICustomFormatService
    {
        Task<List<RadarrCustomFormatData>> GetCustomFormats();
        Task<int> CreateCustomFormat(ProcessedCustomFormatData cf);
        Task UpdateCustomFormat(int formatId, ProcessedCustomFormatData cf);
        Task DeleteCustomFormat(int formatId);
    }
}
