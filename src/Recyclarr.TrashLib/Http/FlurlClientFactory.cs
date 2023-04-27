using Flurl.Http;
using Flurl.Http.Configuration;
using Recyclarr.Common.Networking;
using Recyclarr.TrashLib.Json;
using Recyclarr.TrashLib.Settings;

namespace Recyclarr.TrashLib.Http;

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

    public IFlurlClient BuildClient(Uri baseUrl)
    {
        var client = _factory.Get(baseUrl);
        client.Settings = GetClientSettings();
        return client;
    }

    private ClientFlurlHttpSettings GetClientSettings()
    {
        var settings = new ClientFlurlHttpSettings
        {
            JsonSerializer = new NewtonsoftJsonSerializer(ServiceJsonSerializerFactory.Settings)
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
