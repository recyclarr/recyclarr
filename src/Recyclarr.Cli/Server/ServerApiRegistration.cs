using System.Text.Json;
using System.Text.Json.Serialization;
using Autofac;
using Recyclarr.Client.V1;
using Refit;

namespace Recyclarr.Cli.Server;

internal static class ServerApiRegistration
{
    // The generated contracts carry explicit [JsonPropertyName] attributes, so only enums need
    // configuring here. The server writes them as camelCase strings.
    private static readonly RefitSettings SelfApiRefitSettings = new()
    {
        ContentSerializer = new SystemTextJsonContentSerializer(
            new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            }
        ),
    };

    extension(ContainerBuilder builder)
    {
        public void RegisterServerApi()
        {
            builder.RegisterType<EphemeralServerLauncher>();
            builder.RegisterType<ServerConnectionFactory>();
            builder.RegisterType<HttpClient>();

            builder.Register<Func<Uri, ISyncApi>>(ctx =>
            {
                var createClient = ctx.Resolve<Func<HttpClient>>();
                return baseAddress =>
                {
                    var client = createClient();
                    client.BaseAddress = baseAddress;
                    return RestService.For<ISyncApi>(client, SelfApiRefitSettings);
                };
            });
        }
    }
}
