using System.Text.Json.Serialization;

#pragma warning disable CA1065

namespace Recyclarr.TrashGuide.CustomFormat;

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
