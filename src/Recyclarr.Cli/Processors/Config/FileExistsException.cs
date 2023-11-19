namespace Recyclarr.Cli.Processors.Config;

public class FileExistsException(string attemptedPath) : Exception
{
    public string AttemptedPath { get; } = attemptedPath;
}
