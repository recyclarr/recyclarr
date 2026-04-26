using System.IO.Abstractions;
using Recyclarr.Platform;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Recyclarr.Config.File;

public record FileTag;

public class FileDeserializer(IFileSystem fs, IAppPaths paths) : INodeDeserializer
{
    public bool Deserialize(
        IParser reader,
        Type expectedType,
        Func<IParser, Type, object?> nestedObjectDeserializer,
        out object? value,
        ObjectDeserializer rootDeserializer
    )
    {
        // Only process items flagged as File references
        if (expectedType != typeof(FileTag))
        {
            value = null;
            return false;
        }

        var scalar = reader.Consume<Scalar>();
        var resolvedPath = ResolveFilePath(scalar.Value);
        value = fs.File.ReadAllText(resolvedPath).Trim();
        return true;
    }

    private string ResolveFilePath(string path)
    {
        if (fs.Path.IsPathRooted(path))
        {
            return path;
        }

        return paths.ConfigDirectory.File(path).FullName;
    }
}
