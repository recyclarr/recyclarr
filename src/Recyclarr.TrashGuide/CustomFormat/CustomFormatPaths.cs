using System.IO.Abstractions;

namespace Recyclarr.TrashGuide.CustomFormat;

internal record CustomFormatPaths(
    IReadOnlyList<IDirectoryInfo> CustomFormatDirectories,
    IFileInfo CollectionOfCustomFormatsMarkdown
);
