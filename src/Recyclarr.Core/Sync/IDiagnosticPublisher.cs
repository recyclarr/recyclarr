namespace Recyclarr.Sync;

public interface IDiagnosticPublisher
{
    void Add(SyncOutcome outcome);
    void AddError(string message);
    void AddWarning(string message);
    void AddDeprecation(string message);
}
