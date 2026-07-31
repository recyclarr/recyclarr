using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Http;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.Server;

internal static class OutboundHttpClientRegistration
{
    extension(IServiceCollection services)
    {
        [SuppressMessage(
            "Security",
            "CA5399:HttpClient is created without enabling CheckCertificateRevocationList"
        )]
        [SuppressMessage("Security", "CA5359:Do Not Disable Certificate Validation")]
        public void AddOutboundHttpClients()
        {
            services.AddTransient<HttpLoggingHandler>();

            // Suppress automatic scope creation per client; handler dependencies are all
            // singletons and these scopes would be invisible to the intentional run/instance
            // Autofac hierarchy.
            services.Configure<HttpClientFactoryOptions>(
                "servarr",
                options => options.SuppressHandlerScope = true
            );
            services
                .AddHttpClient("servarr")
                .RemoveAllLoggers()
                .AddHttpMessageHandler<HttpLoggingHandler>()
                .ConfigurePrimaryHttpMessageHandler(sp =>
                {
                    var settings = sp.GetRequiredService<ISettings<RecyclarrSettings>>();
                    var handler = new HttpClientHandler();

                    if (!settings.Value.EnableSslCertificateValidation)
                    {
                        handler.ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                    }

                    return handler;
                });

            services.Configure<HttpClientFactoryOptions>(
                "apprise",
                options => options.SuppressHandlerScope = true
            );
            services
                .AddHttpClient("apprise")
                .RemoveAllLoggers()
                .AddHttpMessageHandler<HttpLoggingHandler>();
        }
    }
}
