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
        value = Read(fs, paths.ConfigDirectory, scalar.Value);
        return true;
    }

    /// <summary>
    /// The trimmed contents of a <c>!file</c> tag's file; relative paths resolve against the
    /// configuration directory.
    /// </summary>
    public static string Read(IFileSystem fs, IDirectoryInfo configDirectory, string path)
    {
        var resolved = fs.Path.IsPathRooted(path) ? path : configDirectory.File(path).FullName;
        return fs.File.ReadAllText(resolved).Trim();
    }
}
