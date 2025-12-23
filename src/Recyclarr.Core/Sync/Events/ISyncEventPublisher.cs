namespace Recyclarr.Sync.Events;

public interface ISyncEventPublisher
{
    void AddError(string message);
    void AddWarning(string message);
    void AddDeprecation(string message);
}
