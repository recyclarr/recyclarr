using System.IO.Abstractions;
using System.Reflection;

namespace Recyclarr.Common.TestLibrary;

public static class CommonMockFileSystemExtensions
{
    public static void AddFileFromEmbeddedResource(
        this MockFileSystem fs,
        IFileInfo path,
        Assembly resourceAssembly,
        string embeddedResourcePath)
    {
        fs.AddFileFromEmbeddedResource(path.FullName, resourceAssembly, embeddedResourcePath);
    }

    public static void AddSameFileFromEmbeddedResource(
        this MockFileSystem fs,
        IFileInfo path,
        Type typeInAssembly,
        string resourceSubPath = "Data")
    {
        fs.AddFileFromEmbeddedResource(path, typeInAssembly, $"{resourceSubPath}.{path.Name}");
    }

    public static void AddSameFileFromEmbeddedResource(
        this MockFileSystem fs,
        string path,
        Type typeInAssembly,
        string resourceSubPath = "Data")
    {
        fs.AddFileFromEmbeddedResource(fs.FileInfo.New(path), typeInAssembly, resourceSubPath);
    }

    public static void AddFileFromEmbeddedResource(
        this MockFileSystem fs,
        IFileInfo path,
        Type typeInAssembly,
        string embeddedResourcePath)
    {
        fs.AddFileFromEmbeddedResource(path.FullName, typeInAssembly, embeddedResourcePath);
    }

    public static void AddFileFromEmbeddedResource(
        this MockFileSystem fs,
        string path,
        Type typeInAssembly,
        string embeddedResourcePath)
    {
        var resourcePath = $"{typeInAssembly.Namespace}.{embeddedResourcePath}";
        fs.AddFileFromEmbeddedResource(path, typeInAssembly.Assembly, resourcePath);
    }

    public static IEnumerable<string> LeafDirectories(this MockFileSystem fs)
    {
        return fs.AllDirectories.Where(x => !fs.AllDirectories.Any(y => y.StartsWith(x) && y != x));
    }
}
