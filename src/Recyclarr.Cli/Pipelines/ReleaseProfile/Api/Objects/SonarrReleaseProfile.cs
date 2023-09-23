using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile.Api.Objects;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.Members)]
public class SonarrPreferredTerm
{
    public SonarrPreferredTerm(int score, string term)
    {
        Term = term;
        Score = score;
    }

    [JsonPropertyName("key")]
    public string Term { get; set; }

    [JsonPropertyName("value")]
    public int Score { get; set; }
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
