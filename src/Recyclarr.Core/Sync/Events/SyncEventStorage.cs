namespace Recyclarr.Sync.Events;

public class SyncEventStorage
{
    private readonly List<DiagnosticEvent> _diagnostics = [];
    private readonly List<CompletionEvent> _completions = [];

    public IReadOnlyList<DiagnosticEvent> Diagnostics => _diagnostics;
    public IReadOnlyList<CompletionEvent> Completions => _completions;
    public IEnumerable<SyncEvent> AllEvents => _diagnostics.Cast<SyncEvent>().Concat(_completions);

    public void Add(SyncEvent evt)
    {
        switch (evt)
        {
            case DiagnosticEvent d:
                _diagnostics.Add(d);
                break;
            case CompletionEvent c:
                _completions.Add(c);
                break;
        }
    }

    public void Clear()
    {
        _diagnostics.Clear();
        _completions.Clear();
    }

    public bool HasInstanceErrors(string instanceName) =>
        _diagnostics.Any(e => e.InstanceName == instanceName && e.Type == DiagnosticType.Error);
}
