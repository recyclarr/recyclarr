using System.IO.Abstractions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Recyclarr.Config.File;

public record FileTag;

public class FileDeserializer(IFileSystem fs) : INodeDeserializer
{
    public bool Deserialize(
        IParser reader,
        Type expectedType,
        Func<IParser, Type, object?> nestedObjectDeserializer,
        out object? value,
        ObjectDeserializer rootDeserializer
    )
    {
        // Only process items flagged as Secrets
        if (expectedType != typeof(FileTag))
        {
            value = null;
            return false;
        }

        var filePath = reader.Consume<Scalar>();
        value = fs.File.ReadAllText(filePath.Value);
        return true;
    }
}
