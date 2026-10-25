using Refit;

namespace Recyclarr.Api.Sonarr.V3;

public partial interface ICustomFormatApi
{
    /// <summary>
    /// Typed replacement for the generated <c>Schema()</c>, which discards the response body because
    /// Sonarr's v3 OpenAPI spec declares none for this endpoint.
    /// </summary>
    /// <remarks>
    /// Remove this method and call the generated <c>Schema()</c> once the vendored spec declares a
    /// response body for <c>GET /api/v3/customformat/schema</c>.
    /// </remarks>
    [Headers("Accept: application/json")]
    [Get("/api/v3/customformat/schema")]
    Task<ICollection<CustomFormatSpecificationSchema>> SpecificationSchema(
        CancellationToken ct = default
    );
}
