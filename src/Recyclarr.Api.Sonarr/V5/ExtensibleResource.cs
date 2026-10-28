using System.Text.Json;
using System.Text.Json.Serialization;

namespace Recyclarr.Api.Sonarr.V5;

/// <summary>
/// Base for every Sonarr v5 API resource. Sonarr returns more fields than Recyclarr models, and
/// adds new ones between releases.
/// </summary>
/// <remarks>
/// Unmodeled fields are kept in <see cref="ExtensionData" /> on read and written back on update,
/// so sending a fetched resource back does not reset settings Recyclarr does not manage. Modeled
/// properties avoid enum types for the same reason: a value added in a newer Sonarr release must
/// not fail deserialization.
/// </remarks>
public abstract class ExtensibleResource
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; set; } = [];
}
