namespace Recyclarr.Cache;

public interface ICacheSyncSource
{
    IEnumerable<TrashIdMapping> SyncedMappings { get; }
    IEnumerable<int> DeletedIds { get; }
    IEnumerable<int> ValidServiceIds { get; }
}
