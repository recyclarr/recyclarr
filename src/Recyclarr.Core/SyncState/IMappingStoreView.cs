namespace Recyclarr.SyncState;

public interface IMappingStoreView
{
    IReadOnlyList<TrashIdMapping> Mappings { get; }
    int? FindId(MappingKey key);
}
