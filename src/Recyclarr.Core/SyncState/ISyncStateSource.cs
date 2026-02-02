namespace Recyclarr.SyncState;

public interface ISyncStateSource
{
    IEnumerable<TrashIdMapping> SyncedMappings { get; }
    IEnumerable<int> DeletedIds { get; }
    IEnumerable<int> ValidServiceIds { get; }
}
