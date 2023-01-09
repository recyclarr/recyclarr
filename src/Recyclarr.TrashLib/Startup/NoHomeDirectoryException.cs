namespace Recyclarr.TrashLib.Startup;

public class NoHomeDirectoryException : Exception
{
    public NoHomeDirectoryException(string msg)
        : base(msg)
    {
    }
}
