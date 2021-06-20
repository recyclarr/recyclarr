using System;
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
        private List<CustomFormatIdentifier>? _customFormatIdentifiers;

        public CustomFormatRepository(IRadarrGuideService guideService)
        {
            _guideService = guideService;
        }

        public IList<CustomFormatIdentifier> Identifiers =>
            _customFormatIdentifiers ?? throw new NullReferenceException(
                "CustomFormatRepository.BuildRepository() must be called before requesting a list of CF identifiers");

        public bool IsLoaded => _customFormatIdentifiers != null;

        public async Task ForceRebuildRepository()
        {
            _customFormatIdentifiers = null;
            await BuildRepository();
        }

        public async Task<bool> BuildRepository()
        {
            if (_customFormatIdentifiers != null)
            {
                return false;
            }

            _customFormatIdentifiers = (await _guideService.GetCustomFormatJson())
                .Select(JObject.Parse)
                .Select(json => new CustomFormatIdentifier((string) json["name"], (string) json["trash_id"]))
                .ToList();

            return true;
        }
    }
}
