using Common.Networking;
using Flurl.Http;
using Flurl.Http.Configuration;
using Newtonsoft.Json;
using Serilog;
using TrashLib.Config.Settings;

namespace TrashLib.Http;

public class FlurlClientFactory : IFlurlClientFactory
{
    private readonly ILogger _log;
    private readonly ISettingsProvider _settingsProvider;
    private readonly PerBaseUrlFlurlClientFactory _factory;

    public FlurlClientFactory(ILogger log, ISettingsProvider settingsProvider)
    {
        _log = log;
        _settingsProvider = settingsProvider;
        _factory = new PerBaseUrlFlurlClientFactory();
    }

    public IFlurlClient Get(string baseUrl)
    {
        var client = _factory.Get(baseUrl);
        client.Settings = GetClientSettings();
        return client;
    }

    private ClientFlurlHttpSettings GetClientSettings()
    {
        var settings = new ClientFlurlHttpSettings
        {
            JsonSerializer = new NewtonsoftJsonSerializer(new JsonSerializerSettings
            {
                // This makes sure that null properties, such as maxSize and preferredSize in Radarr
                // Quality Definitions, do not get written out to JSON request bodies.
                NullValueHandling = NullValueHandling.Ignore
            })
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
