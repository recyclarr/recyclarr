using System.IO.Abstractions;

namespace Recyclarr.Cli.Pipelines.QualitySize.Guide;

internal record QualitySizePaths(
    IReadOnlyCollection<IDirectoryInfo> QualitySizeDirectories
);
