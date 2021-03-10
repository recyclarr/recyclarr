using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Trash.Sonarr.Api.Objects
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.Members)]
    public class SonarrPreferredTerm
    {
        public SonarrPreferredTerm(int score, string term)
        {
            Term = term;
            Score = score;
        }

        [JsonProperty("key")]
        public string Term { get; set; }

        [JsonProperty("value")]
        public int Score { get; set; }
    }

    [UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.Members)]
    public class SonarrReleaseProfile
    {
        public int Id { get; set; }
        public bool Enabled { get; set; }
        public string Name { get; set; } = "";
        public string Required { get; set; } = "";
        public string Ignored { get; set; } = "";
        public List<SonarrPreferredTerm> Preferred { get; set; } = new();
        public bool IncludePreferredWhenRenaming { get; set; }
        public int IndexerId { get; set; }
        public List<int> Tags { get; set; } = new();
    }
}
