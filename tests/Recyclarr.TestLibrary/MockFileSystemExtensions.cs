using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace Recyclarr.TestLibrary;

[SuppressMessage(
    "Design",
    "CA1034:Nested types should not be visible",
    Justification = "https://github.com/dotnet/roslyn-analyzers/issues/7765"
)]
public static class MockFileSystemExtensions
{
    extension(MockFileSystem fs)
    {
        public void AddFileFromEmbeddedResource(
            IFileInfo path,
            Type typeInAssembly,
            string embeddedResourcePath
        )
        {
            fs.AddFileFromEmbeddedResource(path.FullName, typeInAssembly, embeddedResourcePath);
        }

        public void AddFileFromEmbeddedResource(
            string path,
            Type typeInAssembly,
            string embeddedResourcePath
        )
        {
            embeddedResourcePath = embeddedResourcePath.Replace("/", ".", StringComparison.Ordinal);
            var resourcePath = $"{typeInAssembly.Namespace}.{embeddedResourcePath}";
            fs.AddFileFromEmbeddedResource(path, typeInAssembly.Assembly, resourcePath);
        }

        public void AddSameFileFromEmbeddedResource(
            IFileInfo path,
            Type typeInAssembly,
            string resourceSubPath = "Data"
        )
        {
            fs.AddFileFromEmbeddedResource(path, typeInAssembly, $"{resourceSubPath}.{path.Name}");
        }

        public void AddFilesFromEmbeddedNamespace(
            IDirectoryInfo path,
            Type typeInAssembly,
            string embeddedResourcePath
        )
        {
            var replace = embeddedResourcePath.Replace("/", ".", StringComparison.Ordinal);
            embeddedResourcePath = $"{typeInAssembly.Namespace}.{replace}";
            fs.AddFilesFromEmbeddedNamespace(
                path.FullName,
                typeInAssembly.Assembly,
                embeddedResourcePath
            );
        }
    }
}
