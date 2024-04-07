using Flurl.Http;
using Recyclarr.Notifications.Apprise.Dto;
using Recyclarr.Settings;

namespace Recyclarr.Notifications.Apprise;

public class AppriseNotificationApiService(IAppriseRequestBuilder api, ISettingsProvider settingsProvider)
    : IAppriseNotificationApiService
{
    public async Task Notify(string key, AppriseNotification notification)
    {
        var settings = settingsProvider.Settings.Notifications?.Apprise;
        if (settings?.Key is null)
        {
            throw new ArgumentException("No apprise notification settings have been defined");
        }

        await api.Request("notify", settings.Key)
            .PostJsonAsync(notification);
    }
}
