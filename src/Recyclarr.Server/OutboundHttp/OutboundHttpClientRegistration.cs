using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Recyclarr.Settings.Models;

namespace Recyclarr.Server.OutboundHttp;

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
                    var settings = sp.GetRequiredService<IOptions<RecyclarrSettings>>();
                    // A redirect means a wrong base URL or a proxy login page; following it
                    // would turn the problem into an unreadable response
                    var handler = new HttpClientHandler { AllowAutoRedirect = false };

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
