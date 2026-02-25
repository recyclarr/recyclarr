namespace Recyclarr.SyncState;

public interface IGuideResource
{
    string TrashId { get; }
    string Name { get; }
    MappingKey MappingKey { get; }
}
