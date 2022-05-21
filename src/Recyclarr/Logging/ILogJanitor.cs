namespace Recyclarr.Logging;

public interface ILogJanitor
{
    void DeleteOldestLogFiles(int numberOfNewestToKeep);
}
