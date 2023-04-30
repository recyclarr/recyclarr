using System.Runtime.Serialization;

namespace Recyclarr.TrashLib.Cache;

[Serializable]
public class CacheException : Exception
{
    public CacheException(string? message)
        : base(message)
    {
    }

    protected CacheException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
