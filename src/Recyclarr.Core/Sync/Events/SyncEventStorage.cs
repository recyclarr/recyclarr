namespace Recyclarr.Sync.Events;

public class SyncEventStorage
{
    private readonly List<SyncEvent> _events = [];

    public IReadOnlyList<SyncEvent> Events => _events;

    public void Add(SyncEvent evt) => _events.Add(evt);

    public void Clear() => _events.Clear();
}
