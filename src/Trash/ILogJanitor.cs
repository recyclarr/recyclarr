namespace Trash
{
    public interface ILogJanitor
    {
        void DeleteOldestLogFiles(int numberOfNewestToKeep);
    }
}
