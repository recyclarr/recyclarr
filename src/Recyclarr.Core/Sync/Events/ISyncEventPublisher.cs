namespace Recyclarr.Sync.Events;

public interface ISyncEventPublisher
{
    void AddError(string message, Exception? exception = null);
    void AddWarning(string message);
    void AddDeprecation(string message);
}
