using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Guide.QualitySize;

internal record QualitySizePaths(
    IReadOnlyCollection<IDirectoryInfo> QualitySizeDirectories
);
