using System.IO.Abstractions;
using System.Text.Json;
using Recyclarr.Json;

namespace Recyclarr.ResourceProviders.Domain;

public class JsonResourceLoader(IFileSystem fs)
{
    public IEnumerable<(TResource Resource, IFileInfo SourceFile)> Load<TResource>(
        IEnumerable<IFileInfo> files
    )
        where TResource : class
    {
        return files
            .Select(file => (Resource: DeserializeFile<TResource>(file), SourceFile: file))
            .Where(tuple => tuple.Resource is not null)
            .Cast<(TResource, IFileInfo)>();
    }

    private TResource? DeserializeFile<TResource>(IFileInfo file)
        where TResource : class
    {
        var json = fs.File.ReadAllText(file.FullName);
        return JsonSerializer.Deserialize<TResource>(json, GlobalJsonSerializerSettings.Guide);
    }
}
