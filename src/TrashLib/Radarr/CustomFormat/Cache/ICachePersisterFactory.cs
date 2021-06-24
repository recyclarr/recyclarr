using TrashLib.Config;

namespace TrashLib.Radarr.CustomFormat.Cache
{
    public interface ICachePersisterFactory
    {
        ICachePersister Create(IServiceConfiguration config);
    }
}
