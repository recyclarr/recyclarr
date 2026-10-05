using System.Text.Json;
using System.Text.Json.Serialization;
using Recyclarr.Server.Features.Sync.GetResults;

namespace Recyclarr.Server.Persistence;

/// <summary>
/// Stored form of a finished instance's result: its v1 results representation as JSON.
/// </summary>
/// <remarks>
/// Storage is coupled to the v1 results DTO. A breaking change to that DTO needs a data migration
/// of stored rows (or a stored-form version) before old jobs can be read again.
/// </remarks>
internal static class StoredInstanceResult
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() },
        RespectNullableAnnotations = true,
    };

    public static string Serialize(SyncInstanceResultsResponse result) =>
        JsonSerializer.Serialize(result, Options);

    public static SyncInstanceResultsResponse Deserialize(string json) =>
        JsonSerializer.Deserialize<SyncInstanceResultsResponse>(json, Options)
        ?? throw new InvalidOperationException("Stored instance result is null");
}
