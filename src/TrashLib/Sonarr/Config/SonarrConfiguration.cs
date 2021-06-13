using System.Collections.Generic;
using TrashLib.Config;
using TrashLib.Sonarr.QualityDefinition;
using TrashLib.Sonarr.ReleaseProfile;

namespace TrashLib.Sonarr.Config
{
    public class SonarrConfiguration : ServiceConfiguration
    {
        public IList<ReleaseProfileConfig> ReleaseProfiles { get; set; } = new List<ReleaseProfileConfig>();
        public SonarrQualityDefinitionType? QualityDefinition { get; init; }
    }

    public class ReleaseProfileConfig
    {
        // -1 does not map to a valid enumerator. this is to force validation to fail if it is not set from YAML
        // all of this craziness is to avoid making the enum type nullable which will make using the property
        // frustrating.
        public ReleaseProfileType Type { get; init; } = (ReleaseProfileType) (-1);

        public bool StrictNegativeScores { get; init; }
        public SonarrProfileFilterConfig Filter { get; init; } = new();
        public ICollection<string> Tags { get; init; } = new List<string>();
    }

    public class SonarrProfileFilterConfig
    {
        public bool IncludeOptional { get; set; }
        // todo: Add Include & Exclude later (list of strings)
    }
}
