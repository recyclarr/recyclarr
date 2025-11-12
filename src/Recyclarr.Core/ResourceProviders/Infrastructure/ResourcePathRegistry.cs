using System.IO.Abstractions;

namespace Recyclarr.ResourceProviders.Infrastructure;

public class ResourcePathRegistry : IResourcePathRegistry
{
    private readonly Dictionary<Type, List<IFileInfo>> _filesByResourceType = new();

    public void Register<TResource>(IEnumerable<IFileInfo> files)
        where TResource : class
    {
        var key = typeof(TResource);
        if (!_filesByResourceType.ContainsKey(key))
        {
            _filesByResourceType[key] = [];
        }

        _filesByResourceType[key].AddRange(files);
    }

    public IReadOnlyCollection<IFileInfo> GetFiles<TResource>()
        where TResource : class
    {
        var key = typeof(TResource);
        return _filesByResourceType.TryGetValue(key, out var files) ? files : [];
    }
}
