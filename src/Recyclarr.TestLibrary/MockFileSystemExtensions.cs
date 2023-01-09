using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace Recyclarr.TestLibrary;

public static class MockFileSystemExtensions
{
    public static void AddEmptyFile(this MockFileSystem fs, string path)
    {
        fs.AddFile(path, new MockFileData(""));
    }

    public static void AddEmptyFile(this MockFileSystem fs, IFileInfo path)
    {
        fs.AddEmptyFile(path.FullName);
    }

    public static void AddDirectory(this MockFileSystem fs, IDirectoryInfo path)
    {
        fs.AddDirectory(path.FullName);
    }

    public static void AddFile(this MockFileSystem fs, IFileInfo path, MockFileData data)
    {
        fs.AddFile(path.FullName, data);
    }

    public static MockFileData GetFile(this MockFileSystem fs, IFileInfo path)
    {
        return fs.GetFile(path.FullName);
    }
}
