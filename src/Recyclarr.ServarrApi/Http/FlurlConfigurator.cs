using System.Diagnostics.CodeAnalysis;
using System.Net.Security;
using Flurl.Http.Configuration;
using Recyclarr.Json;
using Recyclarr.Settings;
using Serilog;

namespace Recyclarr.ServarrApi.Http;

public class FlurlConfigurator(ILogger log, ISettingsProvider settingsProvider)
{
    [SuppressMessage("SonarCloud", "S4830:Server certificates should be verified during SSL/TLS connections")]
    [SuppressMessage("Security", "CA5359:Do Not Disable Certificate Validation")]
    public void Configure(IFlurlClientBuilder builder)
    {
        builder.WithSettings(settings =>
        {
            settings.JsonSerializer = new DefaultJsonSerializer(GlobalJsonSerializerSettings.Services);
            FlurlLogging.SetupLogging(settings, log);
        });

        builder.ConfigureInnerHandler(handler =>
        {
            if (!settingsProvider.Settings.EnableSslCertificateValidation)
            {
                log.Warning(
                    "Security Risk: Certificate validation is being DISABLED because setting " +
                    "`enable_ssl_certificate_validation` is set to `false`");

                handler.SslOptions = new SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = (_, _, _, _) => true
                };
            }
        });
    }
}
