using System.Text.Json.Serialization;
using Recyclarr.Json;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.ResourceProviders.Domain;

public record CustomFormatResource
{
    [JsonIgnore]
    public string? Category { get; init; }

    [JsonPropertyName("trash_id")]
    [JsonNoSerialize]
    public string TrashId { get; init; } = "";

    [JsonPropertyName("trash_scores")]
    [JsonNoSerialize]
    public Dictionary<string, int> TrashScores { get; init; } =
        new(StringComparer.InvariantCultureIgnoreCase);

    [JsonIgnore]
    public int? DefaultScore => TrashScores.TryGetValue("default", out var score) ? score : null;

    public int Id { get; set; }
    public string Name { get; init; } = "";
    public bool IncludeCustomFormatWhenRenaming { get; init; }
    public IReadOnlyCollection<CustomFormatSpecificationData> Specifications { get; init; } = [];

    public virtual bool Equals(CustomFormatResource? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // FullOuterHashJoin matches specs by name. For matched pairs, delegates to
        // CustomFormatSpecificationData equality (compares Implementation, Negate, Required, Fields).
        // Returns false for any unmatched specs (spec exists only in one side).
        var specsEqual = Specifications
            .FullOuterHashJoin(
                other.Specifications,
                x => x.Name,
                x => x.Name,
                _ => false,
                _ => false,
                (x, y) => x == y
            )
            .All(x => x);

        return Id == other.Id
            && Name == other.Name
            && IncludeCustomFormatWhenRenaming == other.IncludeCustomFormatWhenRenaming
            && specsEqual;
    }

    /// <summary>
    /// Compares custom format data for equivalence, ignoring record type differences.
    /// Use this instead of Equals() when comparing across inheritance hierarchy
    /// (e.g., SonarrCustomFormatResource vs CustomFormatResource from API).
    /// </summary>
    public bool IsEquivalentTo(CustomFormatResource? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // FullOuterHashJoin matches specs by name. For matched pairs, delegates to
        // CustomFormatSpecificationData equality (compares Implementation, Negate, Required, Fields).
        // Returns false for any unmatched specs (spec exists only in one side).
        var specsEqual = Specifications
            .FullOuterHashJoin(
                other.Specifications,
                x => x.Name,
                x => x.Name,
                _ => false,
                _ => false,
                (x, y) => x == y
            )
            .All(x => x);

        return Id == other.Id
            && Name == other.Name
            && IncludeCustomFormatWhenRenaming == other.IncludeCustomFormatWhenRenaming
            && specsEqual;
    }

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(TrashId);
}

public record RadarrCustomFormatResource : CustomFormatResource;

public record SonarrCustomFormatResource : CustomFormatResource;
