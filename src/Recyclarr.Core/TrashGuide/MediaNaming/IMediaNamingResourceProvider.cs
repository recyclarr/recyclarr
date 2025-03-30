using System.IO.Abstractions;
using Recyclarr.ResourceProviders;

namespace Recyclarr.TrashGuide.MediaNaming;

public interface IMediaNamingResourceProvider : IResourceProvider
{
    IEnumerable<IDirectoryInfo> GetMediaNamingPaths(SupportedServices service);
}
