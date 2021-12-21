using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace TrashLib.Sonarr.Api.Objects;

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

// Retained for supporting versions of Sonarr prior to v3.0.6.1355
// Offending change is here:
// https://github.com/Sonarr/Sonarr/blob/deed85d2f9147e6180014507ef4f5af3695b0c61/src/NzbDrone.Core/Datastore/Migration/162_release_profile_to_array.cs
// The Ignored and Required JSON properties were converted from string -> array in a backward-breaking way.
[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.Members)]
public class SonarrReleaseProfileV1
{
    public int Id { get; set; }
    public bool Enabled { get; set; }
    public string Name { get; set; } = "";
    public string Required { get; set; } = "";
    public string Ignored { get; set; } = "";
    public IReadOnlyCollection<SonarrPreferredTerm> Preferred { get; set; } = new List<SonarrPreferredTerm>();
    public bool IncludePreferredWhenRenaming { get; set; }
    public int IndexerId { get; set; }
    public IReadOnlyCollection<int> Tags { get; set; } = new List<int>();
}

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.Members)]
public class SonarrReleaseProfile
{
    public int Id { get; set; }
    public bool Enabled { get; set; }
    public string Name { get; set; } = "";
    public IReadOnlyCollection<string> Required { get; set; } = new List<string>();
    public IReadOnlyCollection<string> Ignored { get; set; } = new List<string>();
    public IReadOnlyCollection<SonarrPreferredTerm> Preferred { get; set; } = new List<SonarrPreferredTerm>();
    public bool IncludePreferredWhenRenaming { get; set; }
    public int IndexerId { get; set; }
    public IReadOnlyCollection<int> Tags { get; set; } = new List<int>();
}
