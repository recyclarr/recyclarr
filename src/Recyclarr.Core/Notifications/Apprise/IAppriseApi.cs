using Recyclarr.Notifications.Apprise.Dto;
using Refit;

namespace Recyclarr.Notifications.Apprise;

internal interface IAppriseApi
{
    [Post("/notify/{key}")]
    Task Notify(string key, [Body] AppriseStatefulNotification notification);

    [Post("/notify")]
    Task Notify([Body] AppriseStatelessNotification notification);
}
