using Refit;

namespace Recyclarr.Api.Sonarr.V3;

/// <summary>Sonarr v3 quality definitions: the global file size limits for each quality.</summary>
public interface IQualityDefinitionApi
{
    [Get("/api/v3/qualitydefinition")]
    Task<ICollection<QualityDefinitionResource>> QualitydefinitionGet(
        CancellationToken ct = default
    );

    [Put("/api/v3/qualitydefinition/update")]
    Task Update(
        [Body] IEnumerable<QualityDefinitionResource>? body,
        CancellationToken ct = default
    );
}

/// <summary>Size limits for one quality, in megabytes per minute of runtime.</summary>
public class QualityDefinitionResource : ExtensibleResource
{
    public int? Id { get; set; }
    public Quality? Quality { get; set; }
    public double? MinSize { get; set; }
    public double? MaxSize { get; set; }
    public double? PreferredSize { get; set; }
}
