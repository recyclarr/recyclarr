namespace Recyclarr;

public interface ILogJanitor
{
    void DeleteOldestLogFiles(int numberOfNewestToKeep);
}
