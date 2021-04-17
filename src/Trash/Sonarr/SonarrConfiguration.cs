using System.Collections.Generic;
using Flurl;
using JetBrains.Annotations;
using Trash.Config;
using Trash.Sonarr.QualityDefinition;
using Trash.Sonarr.ReleaseProfile;

namespace Trash.Sonarr
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class SonarrConfiguration : BaseConfiguration
    {
        public List<ReleaseProfileConfig> ReleaseProfiles { get; set; } = new();
        public SonarrQualityDefinitionType? QualityDefinition { get; init; }

        public override string BuildUrl()
        {
            return BaseUrl
                .AppendPathSegment("api/v3")
                .SetQueryParams(new {apikey = ApiKey});
        }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class ReleaseProfileConfig
    {
        public ReleaseProfileType Type { get; init; }
        public bool StrictNegativeScores { get; init; }
        public List<string> Tags { get; init; } = new();
    }
}
