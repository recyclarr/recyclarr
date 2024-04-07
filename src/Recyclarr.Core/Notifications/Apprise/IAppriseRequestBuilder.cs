using Flurl.Http;

namespace Recyclarr.Notifications.Apprise;

public interface IAppriseRequestBuilder
{
    IFlurlRequest Request(params object[] path);
}
