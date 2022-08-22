using JetBrains.Annotations;
using Newtonsoft.Json;

namespace TrashLib.Services.Sonarr.ReleaseProfile;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record TermData
{
    [JsonProperty("trash_id")]
    public string TrashId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
    public string Term { get; init; } = string.Empty;

    public override string ToString()
    {
        return $"[TrashId: {TrashId}] [Name: {Name}] [Term: {Term}]";
    }
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

    public override string ToString()
    {
        return $"[Score: {Score}] [Terms: {Terms.Count}]";
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

    public override string ToString()
    {
        return $"[TrashId: {TrashId}] " +
               $"[Name: {Name}] " +
               $"[IncludePreferred: {IncludePreferredWhenRenaming}] " +
               $"[Required: {Required.Count}] " +
               $"[Ignored: {Ignored.Count}] " +
               $"[Preferred: {Preferred.Count}]";
    }
}
