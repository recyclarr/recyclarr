using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Recyclarr.TrashLib.Json;

namespace Recyclarr.TrashLib.Tests.Json;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class BulkJsonLoaderTest
{
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
    private sealed record TestJsonObject(string TrashId, int TrashScore, string Name);

    [Test, AutoMockData]
    public void Deserialize_works(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        BulkJsonLoader sut)
    {
        var jsonData =
            """
            {
              "trash_id": "90cedc1fea7ea5d11298bebd3d1d3223",
              "trash_score": "-10000",
              "name": "TheName"
            }
            """;

        fs.AddFile(fs.CurrentDirectory().File("file.json"), new MockFileData(jsonData));

        var result = sut.LoadAllFilesAtPaths<TestJsonObject>(new[] {fs.CurrentDirectory()});

        result.Should().BeEquivalentTo(new[]
        {
            new TestJsonObject("90cedc1fea7ea5d11298bebd3d1d3223", -10000, "TheName")
        });
    }
}
