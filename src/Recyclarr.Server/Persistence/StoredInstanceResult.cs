using System.Text.Json;
using Recyclarr.Server.Api;
using Recyclarr.Server.Features.Sync.GetResults;

namespace Recyclarr.Server.Persistence;

/// <summary>
/// Stored form of a finished instance's result: its v1 results representation as JSON.
/// </summary>
/// <remarks>
/// Storage is coupled to the v1 results DTO. A breaking change to that DTO needs a data migration
/// of stored rows (or a stored-form version) before old jobs can be read again. Rows use the wire
/// JSON format because outcome data is untyped in the DTO: it reads back as raw JSON and is
/// returned to clients exactly as stored.
/// </remarks>
internal static class StoredInstanceResult
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        ServerApi.ConfigureWireJson(options);
        return options;
    }

    public static string Serialize(SyncInstanceResultsResponse result) =>
        JsonSerializer.Serialize(result, Options);

    public static SyncInstanceResultsResponse Deserialize(string json) =>
        JsonSerializer.Deserialize<SyncInstanceResultsResponse>(json, Options)
        ?? throw new InvalidOperationException("Stored instance result is null");
}
