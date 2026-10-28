using Refit;

namespace Recyclarr.Api.Sonarr.V5;

/// <summary>Sonarr v5 quality profile endpoints Recyclarr syncs through.</summary>
public interface IQualityProfileApi
{
    [Get("/api/v5/qualityprofile")]
    Task<ICollection<QualityProfileResource>> QualityprofileGet(CancellationToken ct = default);

    [Post("/api/v5/qualityprofile")]
    Task<QualityProfileResource> QualityprofilePost(
        [Body] QualityProfileResource? body,
        CancellationToken ct = default
    );

    [Put("/api/v5/qualityprofile/{id}")]
    Task<QualityProfileResource> QualityprofilePut(
        string id,
        [Body] QualityProfileResource? body,
        CancellationToken ct = default
    );
}

/// <summary>
/// Sonarr's template for a new quality profile, listing every quality the server knows.
/// </summary>
public interface IQualityProfileSchemaApi
{
    [Get("/api/v5/qualityprofile/schema")]
    Task<QualityProfileResource> Schema(CancellationToken ct = default);
}

/// <summary>Languages Sonarr knows.</summary>
public interface ILanguageApi
{
    [Get("/api/v5/language")]
    Task<ICollection<LanguageResource>> LanguageGet(CancellationToken ct = default);
}

/// <summary>
/// A quality profile: which qualities are allowed, their sizes, and the custom format scores
/// used to rank releases.
/// </summary>
public class QualityProfileResource : ExtensibleResource
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public bool? UpgradeAllowed { get; set; }
    public int? Cutoff { get; set; }
    public ICollection<QualityProfileQualityItemResource>? Items { get; set; }
    public int? MinFormatScore { get; set; }
    public int? CutoffFormatScore { get; set; }
    public int? MinUpgradeFormatScore { get; set; }
    public ICollection<ProfileFormatItemResource>? FormatItems { get; set; }
}

/// <summary>
/// An entry in a profile's quality list: either a single <see cref="Quality" /> or a named group
/// whose <see cref="Items" /> hold the member qualities. Sizes are in megabytes per minute; Sonarr
/// v5 stores them per profile rather than in global quality definitions.
/// </summary>
public class QualityProfileQualityItemResource : ExtensibleResource
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public Quality? Quality { get; set; }
    public ICollection<QualityProfileQualityItemResource>? Items { get; set; }
    public bool? Allowed { get; set; }
    public double? MinSize { get; set; }
    public double? MaxSize { get; set; }
    public double? PreferredSize { get; set; }
}

/// <summary>The score a profile assigns to one custom format, referenced by ID.</summary>
public class ProfileFormatItemResource : ExtensibleResource
{
    public int? Format { get; set; }
    public string? Name { get; set; }
    public int? Score { get; set; }
}

/// <summary>A release quality such as <c>WEBDL-1080p</c>.</summary>
public class Quality : ExtensibleResource
{
    public int? Id { get; set; }
    public string? Name { get; set; }
}

/// <summary>A language Sonarr knows, as listed by the language endpoint.</summary>
public class LanguageResource : ExtensibleResource
{
    public int? Id { get; set; }
    public string? Name { get; set; }
}
