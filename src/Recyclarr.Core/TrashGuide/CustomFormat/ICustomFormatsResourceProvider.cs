using System.IO.Abstractions;
using Recyclarr.ResourceProviders;

namespace Recyclarr.TrashGuide.CustomFormat;

public interface ICustomFormatsResourceProvider : IResourceProvider
{
    IEnumerable<IDirectoryInfo> GetCustomFormatPaths(SupportedServices service);
}
