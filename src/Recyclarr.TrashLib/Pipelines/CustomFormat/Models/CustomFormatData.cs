using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Json;

namespace Recyclarr.TrashLib.Pipelines.CustomFormat.Models;

public record CustomFormatFieldData
{
    public string Name { get; } = nameof(Value).ToCamelCase();
    public object? Value { get; init; }
}

public record CustomFormatSpecificationData
{
    public string Name { get; init; } = "";
    public string Implementation { get; init; } = "";
    public bool Negate { get; init; }
    public bool Required { get; init; }

    [JsonConverter(typeof(FieldsArrayJsonConverter))]
    public CustomFormatFieldData Fields { get; init; } = new();
}

public record CustomFormatData
{
    public static CustomFormatDataEqualityComparer Comparer { get; } = new();

    [JsonIgnore]
    public string? Category { get; init; }

    [JsonIgnore]
    public string FileName { get; init; } = "";

    [JsonProperty("trash_id")]
    [JsonNoSerialize]
    [SuppressMessage("Design", "CA1044:Properties should not be write only",
        Justification = "We want to deserialize but not serialize this property")]
    public string TrashId { internal get; init; } = "";

    [JsonProperty("trash_score")]
    [JsonNoSerialize]
    [SuppressMessage("Design", "CA1044:Properties should not be write only",
        Justification = "We want to deserialize but not serialize this property")]
    public int? TrashScore { internal get; init; }

    public int Id { get; set; }
    public string Name { get; init; } = "";
    public bool IncludeCustomFormatWhenRenaming { get; init; }
    public IReadOnlyCollection<CustomFormatSpecificationData> Specifications { get; init; } =
        Array.Empty<CustomFormatSpecificationData>();
}
