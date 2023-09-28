using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Recyclarr.Cli.Processors;

[Serializable]
public class FatalException : Exception
{
    public FatalException(string? message, Exception? innerException = null)
        : base(message, innerException)
    {
    }

    [UsedImplicitly]
    protected FatalException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
