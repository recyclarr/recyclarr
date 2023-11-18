using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Recyclarr.ServarrApi.ReleaseProfile;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.Members)]
public class SonarrPreferredTerm(int score, string term)
{
    [JsonPropertyName("key")]
    public string Term { get; set; } = term;

    [JsonPropertyName("value")]
    public int Score { get; set; } = score;
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
