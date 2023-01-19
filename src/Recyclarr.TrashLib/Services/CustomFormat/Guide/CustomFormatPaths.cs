using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Services.CustomFormat.Guide;

internal record CustomFormatPaths(
    IReadOnlyList<IDirectoryInfo> CustomFormatDirectories,
    IFileInfo CollectionOfCustomFormatsMarkdown
);
