using System.Text.Json.Serialization;
using Recyclarr.Json;

// CA1065: Do not raise exceptions in unexpected locations
// Justification: Due to complex equivalency logic, hash codes are not possible. Additionally, these types are not
// intended to be used as keys in Dictionary, HashSet, etc.
#pragma warning disable CA1065

namespace Recyclarr.TrashGuide.CustomFormat;

public record CustomFormatData
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

    public virtual bool Equals(CustomFormatData? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

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

    public override int GetHashCode() => throw new NotImplementedException();
}

public record CustomFormatSpecificationData
{
    public string Name { get; init; } = "";
    public string Implementation { get; init; } = "";
    public bool Negate { get; init; }
    public bool Required { get; init; }

    [JsonConverter(typeof(FieldsArrayJsonConverter))]
    public IReadOnlyCollection<CustomFormatFieldData> Fields { get; init; } = [];

    public virtual bool Equals(CustomFormatSpecificationData? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        var fieldsEqual = Fields
            .InnerHashJoin(other.Fields, x => x.Name, x => x.Name, (x, y) => x == y)
            .All(x => x);

        return Name == other.Name
            && Implementation == other.Implementation
            && Negate == other.Negate
            && Required == other.Required
            && fieldsEqual;
    }

    public override int GetHashCode() => throw new NotImplementedException();
}

public record CustomFormatFieldData
{
    public string Name { get; init; } = "";

    [JsonConverter(typeof(NondeterministicValueConverter))]
    public object? Value { get; init; }
}
