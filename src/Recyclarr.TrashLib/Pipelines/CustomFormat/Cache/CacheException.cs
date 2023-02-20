namespace Recyclarr.TrashLib.Pipelines.CustomFormat.Cache;

public class CacheException : Exception
{
    public CacheException(string? message)
        : base(message)
    {
    }
}
