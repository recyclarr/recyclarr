namespace Recyclarr.Sync.Events;

public class SyncEventStorage
{
    private readonly List<DiagnosticEvent> _diagnostics = [];

    public IReadOnlyList<DiagnosticEvent> Diagnostics => _diagnostics;

    public void Add(DiagnosticEvent evt)
    {
        _diagnostics.Add(evt);
    }

    public void Clear()
    {
        _diagnostics.Clear();
    }

    public bool HasInstanceErrors(string instanceName) =>
        _diagnostics.Any(e => e.InstanceName == instanceName && e.Type == DiagnosticType.Error);
}
