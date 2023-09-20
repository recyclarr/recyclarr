using System.Text.Json.Serialization;
using Recyclarr.Common.Extensions;
using Recyclarr.Json;

namespace Recyclarr.TrashLib.Models;

public record CustomFormatFieldData
{
    public string Name { get; } = nameof(Value).ToCamelCase();

    [JsonConverter(typeof(FieldValueConverter))]
    public object? Value { get; init; }
}

public record CustomFormatSpecificationData
{
    public string Name { get; init; } = "";
    public string Implementation { get; init; } = "";
    public bool Negate { get; init; }
    public bool Required { get; init; }

    [JsonConverter(typeof(FieldsArrayJsonConverter))]
    public IReadOnlyCollection<CustomFormatFieldData> Fields { get; init; } = Array.Empty<CustomFormatFieldData>();
}

public record CustomFormatData
{
    public static CustomFormatDataEqualityComparer Comparer { get; } = new();

    [JsonIgnore]
    public string? Category { get; init; }

    [JsonPropertyName("trash_id")]
    [JsonNoSerialize]
    public string TrashId { get; init; } = "";

    [JsonPropertyName("trash_scores")]
    [JsonNoSerialize]
    public Dictionary<string, int> TrashScores { get; init; } = new(StringComparer.InvariantCultureIgnoreCase);

    [JsonIgnore]
    public int? DefaultScore => TrashScores.TryGetValue("default", out var score) ? score : null;

    public int Id { get; set; }
    public string Name { get; init; } = "";
    public bool IncludeCustomFormatWhenRenaming { get; init; }
    public IReadOnlyCollection<CustomFormatSpecificationData> Specifications { get; init; } =
        Array.Empty<CustomFormatSpecificationData>();
}
