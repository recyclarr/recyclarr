namespace Recyclarr.Platform;

public class NoHomeDirectoryException : Exception
{
    public NoHomeDirectoryException(string msg)
        : base(msg)
    {
    }
}
