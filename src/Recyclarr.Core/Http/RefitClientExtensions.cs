using Autofac;
using Recyclarr.Config.Models;
using Recyclarr.Json;
using Refit;

namespace Recyclarr.Http;

public static class RefitClientExtensions
{
    private static readonly RefitSettings ServarrRefitSettings = new()
    {
        ContentSerializer = new SystemTextJsonContentSerializer(
            GlobalJsonSerializerSettings.Services
        ),
        CaptureRequestContent = true,
    };

    extension(ContainerBuilder builder)
    {
        // Registers a Refit interface for a Servarr (Sonarr/Radarr) API endpoint. The HttpClient
        // is configured per-scope with BaseAddress and X-Api-Key from IServiceConfiguration.
        public void RegisterServarrRefitClient<T>()
            where T : class
        {
            builder
                .Register(ctx =>
                {
                    var factory = ctx.Resolve<IHttpClientFactory>();
                    var config = ctx.Resolve<IServiceConfiguration>();
                    var client = factory.CreateClient("servarr");
                    client.BaseAddress = config.BaseUrl;
                    client.DefaultRequestHeaders.Add("X-Api-Key", config.ApiKey);
                    return RestService.For<T>(client, ServarrRefitSettings);
                })
                .As<T>()
                .InstancePerMatchingLifetimeScope("instance");
        }
    }
}
