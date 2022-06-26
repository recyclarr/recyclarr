using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace TestLibrary;

public static class MockFileSystemExtensions
{
    public static void AddFileNoData(this MockFileSystem fs, string path)
    {
        fs.AddFile(FileUtils.NormalizePath(path), new MockFileData(""));
    }

    public static void AddFileNoData(this MockFileSystem fs, IFileInfo path)
    {
        fs.AddFile(path.FullName, new MockFileData(""));
    }

    public static void AddDirectory2(this MockFileSystem fs, string path)
    {
        fs.AddDirectory(FileUtils.NormalizePath(path));
    }

    public static void AddDirectory(this MockFileSystem fs, IDirectoryInfo path)
    {
        fs.AddDirectory(path.FullName);
    }
}
