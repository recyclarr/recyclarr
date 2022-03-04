using System.IO.Abstractions;

namespace Common;

public class FileUtilities : IFileUtilities
{
    private readonly IFileSystem _fileSystem;

    public FileUtilities(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public void DeleteReadOnlyDirectory(string directory)
    {
        if (!_fileSystem.Directory.Exists(directory))
        {
            return;
        }

        foreach (var subdirectory in _fileSystem.Directory.EnumerateDirectories(directory))
        {
            DeleteReadOnlyDirectory(subdirectory);
        }

        foreach (var fileName in Directory.EnumerateFiles(directory))
        {
            var fileInfo = _fileSystem.FileInfo.FromFileName(fileName);
            fileInfo.Attributes = FileAttributes.Normal;
            fileInfo.Delete();
        }

        _fileSystem.Directory.Delete(directory);
    }
}
