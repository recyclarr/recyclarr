using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Services.QualitySize.Guide;

internal record QualitySizePaths(
    IReadOnlyCollection<IDirectoryInfo> QualitySizeDirectories
);
