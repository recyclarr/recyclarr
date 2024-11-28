using System.Diagnostics.CodeAnalysis;
using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;
using Recyclarr.Config.Models;
using Recyclarr.Http;
using Recyclarr.Json;
using Recyclarr.Settings;

namespace Recyclarr.ServarrApi;

public class ServarrRequestBuilder(
    ILogger log,
    IFlurlClientCache clientCache,
    ISettings<RecyclarrSettings> settings,
    IEnumerable<FlurlSpecificEventHandler> eventHandlers,
    IServiceConfiguration config
) : IServarrRequestBuilder
{
    public IFlurlRequest Request(params object[] path)
    {
        var client = clientCache.GetOrAdd(
            config.InstanceName,
            config.BaseUrl.AppendPathSegments("api", "v3"),
            Configure
        );

        return client.Request(path).WithHeader("X-Api-Key", config.ApiKey);
    }

    [SuppressMessage(
        "SonarCloud",
        "S4830:Server certificates should be verified during SSL/TLS connections"
    )]
    [SuppressMessage("Security", "CA5359:Do Not Disable Certificate Validation")]
    private void Configure(IFlurlClientBuilder builder)
    {
        foreach (var handler in eventHandlers.Select(x => (x.EventType, x)))
        {
            builder.EventHandlers.Add(handler);
        }

        builder.WithSettings(httpSettings =>
        {
            httpSettings.JsonSerializer = new DefaultJsonSerializer(
                GlobalJsonSerializerSettings.Services
            );
        });

        if (!settings.Value.EnableSslCertificateValidation)
        {
            builder.ConfigureInnerHandler(handler =>
            {
                log.Warning(
                    "Security Risk: Certificate validation is being DISABLED because setting "
                        + "`enable_ssl_certificate_validation` is set to `false`"
                );

                handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
            });
        }
    }
}
