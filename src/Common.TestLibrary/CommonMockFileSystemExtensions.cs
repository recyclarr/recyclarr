using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection;

namespace Common.TestLibrary;

public static class CommonMockFileSystemExtensions
{
    public static void AddFileFromResource(this MockFileSystem fs, string resourceFilename)
    {
        fs.AddFileFromResource(resourceFilename, resourceFilename, Assembly.GetCallingAssembly());
    }

    public static void AddFileFromResource(this MockFileSystem fs, IFileInfo file, string resourceFilename,
        string resourceDir = "Data")
    {
        fs.AddFileFromResource(file.FullName, resourceFilename, Assembly.GetCallingAssembly(), resourceDir);
    }

    public static void AddFileFromResource(this MockFileSystem fs, string file, string resourceFilename,
        string resourceDir = "Data")
    {
        fs.AddFileFromResource(file, resourceFilename, Assembly.GetCallingAssembly(), resourceDir);
    }

    public static void AddFileFromResource(this MockFileSystem fs, string file, string resourceFilename,
        Assembly assembly, string resourceDir = "Data")
    {
        var resourceReader = new ResourceDataReader(assembly, resourceDir);
        fs.AddFile(file, new MockFileData(resourceReader.ReadData(resourceFilename)));
    }
}
