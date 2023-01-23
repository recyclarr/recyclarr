using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Pipelines.QualitySize.Guide;

internal record QualitySizePaths(
    IReadOnlyCollection<IDirectoryInfo> QualitySizeDirectories
);
