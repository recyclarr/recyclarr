using System.Text.Json;
using System.Text.Json.Serialization;

namespace Recyclarr.Api.Radarr;

/// <summary>
/// Base for every Radarr API resource. Radarr returns more fields than Recyclarr models, and
/// adds new ones between releases.
/// </summary>
/// <remarks>
/// Unmodeled fields are kept in <see cref="ExtensionData" /> on read and written back on update,
/// so sending a fetched resource back does not reset settings Recyclarr does not manage. Modeled
/// properties avoid enum types for the same reason: a value added in a newer Radarr release must
/// not fail deserialization.
/// </remarks>
public abstract class ExtensibleResource
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; set; } = [];
}
