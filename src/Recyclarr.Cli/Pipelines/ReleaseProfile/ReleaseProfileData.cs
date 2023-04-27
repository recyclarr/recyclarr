using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record TermData
{
    [JsonProperty("trash_id")]
    public string TrashId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
    public string Term { get; init; } = string.Empty;
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record PreferredTermData
{
    public int Score { get; init; }
    public IReadOnlyCollection<TermData> Terms { get; init; } = Array.Empty<TermData>();

    public void Deconstruct(out int score, out IReadOnlyCollection<TermData> terms)
    {
        score = Score;
        terms = Terms;
    }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record ReleaseProfileData
{
    [JsonProperty("trash_id")]
    public string TrashId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
    public bool IncludePreferredWhenRenaming { get; init; }
    public IReadOnlyCollection<TermData> Required { get; init; } = Array.Empty<TermData>();
    public IReadOnlyCollection<TermData> Ignored { get; init; } = Array.Empty<TermData>();
    public IReadOnlyCollection<PreferredTermData> Preferred { get; init; } = Array.Empty<PreferredTermData>();
}
