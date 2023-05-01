using System.Runtime.Serialization;

namespace Recyclarr.TrashLib.Config.Parsing.ErrorHandling;

[Serializable]
public class NoConfigurationFilesException : Exception
{
    public NoConfigurationFilesException()
        : base("No configuration YAML files found")
    {
    }

    protected NoConfigurationFilesException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
