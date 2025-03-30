using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl.Http;
using Flurl.Http.Configuration;
using Recyclarr.Http;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.Notifications.Apprise;

public sealed class AppriseRequestBuilder(
    IFlurlClientCache clientCache,
    ISettings<NotificationSettings> notificationSettings,
    IEnumerable<FlurlSpecificEventHandler> eventHandlers
) : IAppriseRequestBuilder
{
    public IFlurlRequest Request(params object[] path)
    {
        var apprise = notificationSettings.Value.Apprise;
        if (apprise is null)
        {
            throw new ArgumentException("No apprise notification settings have been defined");
        }

        var client = clientCache.GetOrAdd("apprise", apprise.BaseUrl.ToString(), Configure);
        return client.Request(path);
    }

    private void Configure(IFlurlClientBuilder builder)
    {
        foreach (var handler in eventHandlers.Select(x => (x.EventType, x)))
        {
            builder.EventHandlers.Add(handler);
        }

        builder.WithSettings(settings =>
        {
            settings.JsonSerializer = new DefaultJsonSerializer(
                new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = false,
                    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
                }
            );
        });
    }
}
