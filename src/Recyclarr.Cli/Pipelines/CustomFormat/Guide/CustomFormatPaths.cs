using System.IO.Abstractions;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Guide;

internal record CustomFormatPaths(
    IReadOnlyList<IDirectoryInfo> CustomFormatDirectories,
    IFileInfo CollectionOfCustomFormatsMarkdown
);
