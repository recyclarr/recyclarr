using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Guide.CustomFormat;

internal record CustomFormatPaths(
    IReadOnlyList<IDirectoryInfo> CustomFormatDirectories,
    IFileInfo CollectionOfCustomFormatsMarkdown
);
