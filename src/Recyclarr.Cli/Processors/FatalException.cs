using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Recyclarr.Cli.Processors;

[Serializable]
public class FatalException : Exception
{
    public FatalException(string? message)
        : base(message)
    {
    }

    [UsedImplicitly]
    protected FatalException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
