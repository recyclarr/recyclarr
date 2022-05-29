using System.IO.Abstractions.TestingHelpers;

namespace TestLibrary;

public static class MockFileSystemExtensions
{
    public static void AddFileNoData(this MockFileSystem fs, string path)
    {
        fs.AddFile(FileUtils.NormalizePath(path), new MockFileData(""));
    }

    public static void AddDirectory2(this MockFileSystem fs, string path)
    {
        fs.AddDirectory(FileUtils.NormalizePath(path));
    }
}
