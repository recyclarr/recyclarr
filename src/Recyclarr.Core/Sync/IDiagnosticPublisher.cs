namespace Recyclarr.Sync;

public interface IDiagnosticPublisher
{
    void AddError(string message);
    void AddWarning(string message);
    void AddDeprecation(string message);
}
