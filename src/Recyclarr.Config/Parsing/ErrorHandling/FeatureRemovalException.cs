using System.Runtime.Serialization;

namespace Recyclarr.Config.Parsing.ErrorHandling;

[Serializable]
public class FeatureRemovalException : Exception
{
    protected FeatureRemovalException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    public FeatureRemovalException(string message, string docLink)
        : base($"{message} See: {docLink}")
    {
    }
}
