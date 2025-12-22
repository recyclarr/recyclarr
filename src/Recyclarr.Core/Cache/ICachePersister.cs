namespace Recyclarr.Cache;

public interface ICachePersister<TCacheObject>
    where TCacheObject : CacheObject, ITrashIdCacheObject
{
    TrashIdCache<TCacheObject> Load();
    void Save(TrashIdCache<TCacheObject> cache);
}
