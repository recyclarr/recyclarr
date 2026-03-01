using Autofac;
using Recyclarr.Config.Models;
using Recyclarr.Json;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;
using Refit;

namespace Recyclarr.Http;

public static class RefitClientExtensions
{
    private static readonly RefitSettings ServarrRefitSettings = new()
    {
        ContentSerializer = new SystemTextJsonContentSerializer(
            GlobalJsonSerializerSettings.Services
        ),
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

        // Registers a Refit interface for the Apprise notification API. The HttpClient
        // is configured per-scope with BaseAddress from notification settings.
        public void RegisterAppriseRefitClient<T>()
            where T : class
        {
            builder
                .Register(ctx =>
                {
                    var factory = ctx.Resolve<IHttpClientFactory>();
                    var settings = ctx.Resolve<ISettings<NotificationSettings>>().Value;
                    var apprise =
                        settings.Apprise
                        ?? throw new InvalidOperationException(
                            "No Apprise notification settings have been defined"
                        );

                    var client = factory.CreateClient("apprise");
                    client.BaseAddress = apprise.BaseUrl;
                    return RestService.For<T>(client, AppriseRefitSettings);
                })
                .As<T>()
                .InstancePerMatchingLifetimeScope("instance");
        }
    }

    private static readonly RefitSettings AppriseRefitSettings = new()
    {
        ContentSerializer = new SystemTextJsonContentSerializer(
            GlobalJsonSerializerSettings.Apprise
        ),
    };
}
