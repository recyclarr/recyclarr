using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Text.Json;

namespace Recyclarr.ResourceProviders.Domain;

public class JsonResourceLoader
{
    private readonly ConcurrentDictionary<(string Path, Type Type), object> _cache = new();

    public IEnumerable<(TResource Resource, IFileInfo SourceFile)> Load<TResource>(
        IEnumerable<IFileInfo> files,
        JsonSerializerOptions options
    )
        where TResource : class
    {
        return files
            .Select(file => (Resource: GetOrLoad<TResource>(file, options), SourceFile: file))
            .Where(tuple => tuple.Resource is not null)
            .Cast<(TResource, IFileInfo)>();
    }

    private TResource? GetOrLoad<TResource>(IFileInfo file, JsonSerializerOptions options)
        where TResource : class
    {
        var key = (file.FullName, typeof(TResource));

        if (_cache.TryGetValue(key, out var cached))
        {
            return (TResource)cached;
        }

        using var stream = file.OpenRead();
        var result = JsonSerializer.Deserialize<TResource>(stream, options);

        if (result is not null)
        {
            _cache.TryAdd(key, result);
        }

        return result;
    }
}
