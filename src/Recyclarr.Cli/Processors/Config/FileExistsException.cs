namespace Recyclarr.Cli.Processors.Config;

internal class FileExistsException(string attemptedPath)
    : Exception($"File already exists: {attemptedPath}")
{
    public string AttemptedPath { get; } = attemptedPath;
}
