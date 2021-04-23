using System.Collections.Generic;
using System.Linq;

namespace Trash.Sonarr.ReleaseProfile
{
    public class FilteredProfileData
    {
        private readonly ReleaseProfileConfig _config;
        private readonly ProfileData _profileData;

        public FilteredProfileData(ProfileData profileData, ReleaseProfileConfig config)
        {
            _profileData = profileData;
            _config = config;
        }

        public List<string> Required => _config.Filter.IncludeOptional
            ? _profileData.Required.Concat(_profileData.Optional.Required).ToList()
            : _profileData.Required;

        public List<string> Ignored => _config.Filter.IncludeOptional
            ? _profileData.Ignored.Concat(_profileData.Optional.Ignored).ToList()
            : _profileData.Ignored;

        public Dictionary<int, List<string>> Preferred => _config.Filter.IncludeOptional
            ? _profileData.Preferred
                .Union(_profileData.Optional.Preferred)
                .GroupBy(kvp => kvp.Key)
                .ToDictionary(grp => grp.Key, grp => new List<string>(grp.SelectMany(l => l.Value)))
            : _profileData.Preferred;

        public bool? IncludePreferredWhenRenaming => _profileData.IncludePreferredWhenRenaming;
    }
}
