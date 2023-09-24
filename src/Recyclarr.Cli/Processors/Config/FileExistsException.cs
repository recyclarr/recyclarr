namespace Recyclarr.Cli.Processors.Config;

public class FileExistsException : Exception
{
    public FileExistsException(string attemptedPath)
    {
        AttemptedPath = attemptedPath;
    }

    public string AttemptedPath { get; }
}
