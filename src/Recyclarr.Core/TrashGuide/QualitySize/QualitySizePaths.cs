using System.IO.Abstractions;

namespace Recyclarr.TrashGuide.QualitySize;

internal record QualitySizePaths(
    IReadOnlyCollection<IDirectoryInfo> QualitySizeDirectories
);
