using Flurl.Http;

namespace TrashLib.Config
{
    public interface IServerInfo
    {
        IFlurlRequest BuildRequest();
    }
}
