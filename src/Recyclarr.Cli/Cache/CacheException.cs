using System.Runtime.Serialization;

namespace Recyclarr.Cli.Cache;

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
