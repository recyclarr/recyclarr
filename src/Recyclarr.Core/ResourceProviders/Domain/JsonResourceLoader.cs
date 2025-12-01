using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Text.Json;

namespace Recyclarr.ResourceProviders.Domain;

[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Used for DI")]
public class JsonResourceLoader
{
    public IEnumerable<(TResource Resource, IFileInfo SourceFile)> Load<TResource>(
        IEnumerable<IFileInfo> files,
        JsonSerializerOptions options
    )
        where TResource : class
    {
        return files
            .Select(file => (Resource: DeserializeFile<TResource>(file, options), SourceFile: file))
            .Where(tuple => tuple.Resource is not null)
            .Cast<(TResource, IFileInfo)>();
    }

    private static TResource? DeserializeFile<TResource>(
        IFileInfo file,
        JsonSerializerOptions options
    )
        where TResource : class
    {
        using var stream = file.OpenRead();
        return JsonSerializer.Deserialize<TResource>(stream, options);
    }
}
