using System.IO.Abstractions;
using Recyclarr.Config.Parsing;
using Recyclarr.Platform;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Recyclarr.Config.File;

public record FileTag;

public class FileDeserializer(IFileSystem fs, IAppPaths paths, ConfigInputRecorder inputs)
    : INodeDeserializer
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
        var resolved = Resolve(fs, paths.ConfigDirectory, scalar.Value);
        inputs.Record(resolved);
        value = fs.File.ReadAllText(resolved).Trim();
        return true;
    }

    /// <summary>
    /// The trimmed contents of a <c>!file</c> tag's file; relative paths resolve against the
    /// configuration directory.
    /// </summary>
    public static string Read(IFileSystem fs, IDirectoryInfo configDirectory, string path) =>
        fs.File.ReadAllText(Resolve(fs, configDirectory, path)).Trim();

    private static string Resolve(IFileSystem fs, IDirectoryInfo configDirectory, string path) =>
        fs.Path.IsPathRooted(path) ? path : configDirectory.File(path).FullName;
}
