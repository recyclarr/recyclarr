using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TrashLib.Radarr.CustomFormat.Guide;

namespace Recyclarr.Code.Radarr
{
    public class CustomFormatIdentifier
    {
        public CustomFormatIdentifier(string name, string trashId)
        {
            Name = name;
            TrashId = trashId;
        }

        public string Name { get; }
        public string TrashId { get; }
    }

    public class CustomFormatRepository
    {
        private readonly IRadarrGuideService _guideService;
        private readonly List<CustomFormatIdentifier> _customFormatIdentifiers = new();

        public CustomFormatRepository(IRadarrGuideService guideService)
        {
            _guideService = guideService;
        }

        public IList<CustomFormatIdentifier> Identifiers => _customFormatIdentifiers;
        public bool IsLoaded { get; private set; }

        public async Task ForceRebuildRepository()
        {
            IsLoaded = false;
            _customFormatIdentifiers.Clear();
            await BuildRepository();
        }

        public async Task<bool> BuildRepository()
        {
            if (!IsLoaded)
            {
                _customFormatIdentifiers.AddRange((await _guideService.GetCustomFormatJson())
                    .Select(JObject.Parse)
                    .Select(json => new CustomFormatIdentifier((string) json["name"], (string) json["trash_id"])));

                IsLoaded = true;
                return true;
            }

            return false;
        }
    }
}
