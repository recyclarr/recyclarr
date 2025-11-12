using System.IO.Abstractions;

namespace Recyclarr.ResourceProviders.Infrastructure;

public interface IResourcePathRegistry
{
    void Register<TResource>(IEnumerable<IFileInfo> files)
        where TResource : class;
    IReadOnlyCollection<IFileInfo> GetFiles<TResource>()
        where TResource : class;
}
