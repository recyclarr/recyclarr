namespace Recyclarr.Cli.Logging;

public interface ILogJanitor
{
    void DeleteOldestLogFiles(int numberOfNewestToKeep);
}
