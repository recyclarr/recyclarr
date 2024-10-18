namespace Recyclarr.Cache;

public interface ICachePersister<TCache>
{
    TCache Load();
    void Save(TCache cache);
}
