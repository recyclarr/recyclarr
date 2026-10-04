using System.Text.Json;
using System.Text.Json.Serialization;
using Recyclarr.Client.V1;
using Refit;

namespace Recyclarr.Cli.Server;

/// <summary>
/// A command's connection to a Recyclarr server and the generated API clients bound to it.
/// Disposal shuts down the server when this connection started one; for a server the user runs
/// themselves, disposal does nothing.
/// </summary>
internal sealed class ServerConnection(HttpClient client, IAsyncDisposable? ownedServer)
    : IAsyncDisposable
{
    /// <summary>
    /// The generated contracts carry explicit [JsonPropertyName] attributes, so only enums and
    /// nulls need configuring here. The server writes enums as camelCase strings. Optional
    /// request fields generate as nullable, and the server rejects an explicit null for a
    /// non-nullable field, so unset fields are omitted. Responses get the same null rule, so a
    /// response that breaks the contract fails at deserialization instead of later.
    /// </summary>
    private static readonly RefitSettings SelfApiRefitSettings = new()
    {
        ContentSerializer = new SystemTextJsonContentSerializer(
            new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                RespectNullableAnnotations = true,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            }
        ),
    };

    public ISyncApi Sync { get; } = RestService.For<ISyncApi>(client, SelfApiRefitSettings);
    public IGuideApi Guide { get; } = RestService.For<IGuideApi>(client, SelfApiRefitSettings);

    public IInstancesApi Instances { get; } =
        RestService.For<IInstancesApi>(client, SelfApiRefitSettings);

    public ValueTask DisposeAsync()
    {
        return ownedServer?.DisposeAsync() ?? ValueTask.CompletedTask;
    }
}
