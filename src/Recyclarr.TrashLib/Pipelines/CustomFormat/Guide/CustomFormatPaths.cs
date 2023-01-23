using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Pipelines.CustomFormat.Guide;

internal record CustomFormatPaths(
    IReadOnlyList<IDirectoryInfo> CustomFormatDirectories,
    IFileInfo CollectionOfCustomFormatsMarkdown
);
