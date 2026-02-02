namespace Recyclarr.SyncState;

public interface ISyncStatePersister<TStateObject>
    where TStateObject : SyncStateObject, ITrashIdMappings
{
    TrashIdMappingStore<TStateObject> Load();
    void Save(TrashIdMappingStore<TStateObject> store);
}
