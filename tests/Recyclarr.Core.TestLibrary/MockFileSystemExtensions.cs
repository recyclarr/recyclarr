using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using Recyclarr.Core.TestLibrary.Yaml;

namespace Recyclarr.Core.TestLibrary;

[SuppressMessage(
    "Design",
    "CA1034:Nested types should not be visible",
    Justification = "https://github.com/dotnet/roslyn-analyzers/issues/7765"
)]
public static class MockFileSystemExtensions
{
    extension(MockFileSystem fs)
    {
        public void AddYamlFile<T>(IFileInfo path, T content)
        {
            fs.AddFile(path, YamlTestSerializer.ToMockYaml(content));
        }

        public void AddYamlFile<T>(string path, T content)
        {
            fs.AddFile(path, YamlTestSerializer.ToMockYaml(content));
        }

        public void AddJsonFile<T>(IFileInfo path, T content, JsonSerializerOptions options)
        {
            fs.AddFile(path, new MockFileData(JsonSerializer.Serialize(content, options)));
        }

        public void AddJsonFile<T>(string path, T content, JsonSerializerOptions options)
        {
            fs.AddFile(path, new MockFileData(JsonSerializer.Serialize(content, options)));
        }
    }
}
