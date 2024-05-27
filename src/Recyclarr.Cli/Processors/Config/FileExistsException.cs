namespace Recyclarr.Cli.Processors.Config;

public class FileExistsException(string attemptedPath) : Exception($"File already exists: {attemptedPath}")
{
    public string AttemptedPath { get; } = attemptedPath;
}
