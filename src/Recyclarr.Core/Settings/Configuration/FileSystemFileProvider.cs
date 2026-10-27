using System.IO.Abstractions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Recyclarr.Settings.Configuration;

/// <summary>
/// Serves files from one directory of an <see cref="IFileSystem"/>, so file-based configuration
/// sources read through the same filesystem port as the rest of the application.
/// </summary>
/// <remarks>
/// Only single files are served: directory listings are empty and change tokens never fire, which
/// is enough for configuration that loads once and does not reload on change.
/// </remarks>
internal sealed class FileSystemFileProvider(IDirectoryInfo root) : IFileProvider
{
    public Microsoft.Extensions.FileProviders.IFileInfo GetFileInfo(string subpath)
    {
        var file = root.File(subpath.TrimStart('/', '\\'));
        return file.Exists ? new FileEntry(file) : new NotFoundFileInfo(subpath);
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        return NotFoundDirectoryContents.Singleton;
    }

    public IChangeToken Watch(string filter)
    {
        return NullChangeToken.Singleton;
    }

    private sealed class FileEntry(System.IO.Abstractions.IFileInfo file)
        : Microsoft.Extensions.FileProviders.IFileInfo
    {
        public bool Exists => true;
        public long Length => file.Length;

        // Null tells consumers to read through CreateReadStream, not a physical path.
        public string? PhysicalPath => null;
        public string Name => file.Name;
        public DateTimeOffset LastModified => file.LastWriteTimeUtc;
        public bool IsDirectory => false;

        public Stream CreateReadStream()
        {
            return file.OpenRead();
        }
    }
}
