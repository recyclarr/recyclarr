using Refit;

namespace Recyclarr.Api.Sonarr.V3;

/// <summary>Sonarr v3 custom format endpoints Recyclarr syncs through.</summary>
public interface ICustomFormatApi
{
    [Get("/api/v3/customformat")]
    Task<ICollection<CustomFormatResource>> CustomformatGet(CancellationToken ct = default);

    [Post("/api/v3/customformat")]
    Task<CustomFormatResource> CustomformatPost(
        [Body] CustomFormatResource? body,
        CancellationToken ct = default
    );

    [Put("/api/v3/customformat/{id}")]
    Task<CustomFormatResource> CustomformatPut(
        string id,
        [Body] CustomFormatResource? body,
        CancellationToken ct = default
    );

    [Delete("/api/v3/customformat/{id}")]
    Task CustomformatDelete(int id, CancellationToken ct = default);

    /// <summary>
    /// One template per specification implementation. Saved specifications start from these so
    /// members the guide does not carry keep the service's values.
    /// </summary>
    [Get("/api/v3/customformat/schema")]
    Task<ICollection<CustomFormatSpecificationSchema>> Schema(CancellationToken ct = default);
}

/// <summary>
/// A custom format: a named set of specifications that Sonarr scores releases against.
/// </summary>
public class CustomFormatResource : ExtensibleResource
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public bool? IncludeCustomFormatWhenRenaming { get; set; }
    public ICollection<CustomFormatSpecificationSchema>? Specifications { get; set; }
}

/// <summary>
/// One condition of a custom format. <see cref="Implementation" /> names the Sonarr condition
/// type (for example <c>ReleaseTitleSpecification</c>); <see cref="Fields" /> hold its settings.
/// </summary>
public class CustomFormatSpecificationSchema : ExtensibleResource
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public string? Implementation { get; set; }
    public string? ImplementationName { get; set; }
    public string? InfoLink { get; set; }
    public bool? Negate { get; set; }
    public bool? Required { get; set; }
    public ICollection<Field>? Fields { get; set; }
}

/// <summary>
/// A named setting of a custom format specification. <see cref="Value" /> deserializes as a
/// <c>JsonElement</c> because its JSON type depends on the specification.
/// </summary>
public class Field : ExtensibleResource
{
    public string? Name { get; set; }
    public object? Value { get; set; }
}
