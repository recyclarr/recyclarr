using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;
using Recyclarr.Common.Networking;
using Recyclarr.Json;
using Recyclarr.Settings;
using Serilog;

namespace Recyclarr.ServarrApi.Http;

public sealed class FlurlClientFactory : IFlurlClientFactory
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

    public IFlurlClient Get(Url url)
    {
        var client = _factory.Get(url);
        client.Settings = GetClientSettings();
        return client;
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

    public void Dispose()
    {
        _factory.Dispose();
    }
}
