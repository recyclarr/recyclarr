using System.IO.Abstractions;
using Recyclarr.ResourceProviders;

namespace Recyclarr.TrashGuide.QualitySize;

public interface IQualitySizeResourceProvider : IResourceProvider
{
    IEnumerable<IDirectoryInfo> GetQualitySizePaths(SupportedServices service);
}
