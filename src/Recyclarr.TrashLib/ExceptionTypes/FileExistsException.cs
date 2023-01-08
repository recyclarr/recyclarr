namespace Recyclarr.TrashLib.ExceptionTypes;

public class FileExistsException : Exception
{
    public FileExistsException(string attemptedPath)
    {
        AttemptedPath = attemptedPath;
    }

    public string AttemptedPath { get; }
}
