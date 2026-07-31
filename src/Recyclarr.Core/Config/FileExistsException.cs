namespace Recyclarr.Config;

public class FileExistsException(string attemptedPath)
    : Exception($"File already exists: {attemptedPath}")
{
    public string AttemptedPath { get; } = attemptedPath;
}
