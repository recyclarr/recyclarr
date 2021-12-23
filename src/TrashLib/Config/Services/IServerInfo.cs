using Flurl.Http;

namespace TrashLib.Config.Services;

public interface IServerInfo
{
    IFlurlRequest BuildRequest();
}
