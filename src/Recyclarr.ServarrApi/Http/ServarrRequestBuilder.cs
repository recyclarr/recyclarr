using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;
using Recyclarr.Common.Networking;
using Recyclarr.Config.Models;
using Recyclarr.Json;
using Recyclarr.Settings;
using Serilog;

namespace Recyclarr.ServarrApi.Http;

public class ServarrRequestBuilder : IServarrRequestBuilder
{
    private readonly ILogger _log;
    private readonly ISettingsProvider _settingsProvider;
    private readonly IFlurlClientFactory _clientFactory;

    public ServarrRequestBuilder(ILogger log, ISettingsProvider settingsProvider, IFlurlClientFactory clientFactory)
    {
        _log = log;
        _settingsProvider = settingsProvider;
        _clientFactory = clientFactory;
    }

    public IFlurlRequest Request(IServiceConfiguration config, params object[] path)
    {
        var client = _clientFactory.Get(config.BaseUrl.AppendPathSegments("api", "v3"));
        client.Settings = GetClientSettings();
        return client.Request(path)
            .WithHeader("X-Api-Key", config.ApiKey);
    }

    private ClientFlurlHttpSettings GetClientSettings()
    {
        var settings = new ClientFlurlHttpSettings
        {
            JsonSerializer = new DefaultJsonSerializer(GlobalJsonSerializerSettings.Services)
        };

        FlurlLogging.SetupLogging(settings, _log);

        // ReSharper disable once InvertIf
        if (!_settingsProvider.Settings.EnableSslCertificateValidation)
        {
            _log.Warning(
                "Security Risk: Certificate validation is being DISABLED because setting " +
                "`enable_ssl_certificate_validation` is set to `false`");
            settings.HttpClientFactory = new UntrustedCertClientFactory();
        }

        return settings;
    }
}
