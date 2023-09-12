using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Recyclarr.TrashLib.Json;
using Recyclarr.TrashLib.Models;
using Recyclarr.TrashLib.TestLibrary;

namespace Recyclarr.TrashLib.Tests.Json;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class BulkJsonLoaderIntegrationTest : TrashLibIntegrationFixture
{
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
    private sealed record TestJsonObject(string TrashId, int TrashScore, string Name);

    [Test]
    public void Guide_deserialize_works()
    {
        var sut = Resolve<GuideJsonLoader>();

        const string jsonData =
            """
            {
              "trash_id": "90cedc1fea7ea5d11298bebd3d1d3223",
              "trash_score": "-10000",
              "name": "TheName"
            }
            """;

        Fs.AddFile(Fs.CurrentDirectory().File("file.json"), new MockFileData(jsonData));

        var result = sut.LoadAllFilesAtPaths<TestJsonObject>(new[] {Fs.CurrentDirectory()});

        result.Should().BeEquivalentTo(new[]
        {
            new TestJsonObject("90cedc1fea7ea5d11298bebd3d1d3223", -10000, "TheName")
        });
    }

    [Test]
    public void Service_deserialize_works()
    {
        var sut = Resolve<ServiceJsonLoader>();

        const string jsonData =
            """
            {
              "id": 22,
              "name": "FUNi",
              "includeCustomFormatWhenRenaming": true
            }
            """;

        Fs.AddFile(Fs.CurrentDirectory().File("file.json"), new MockFileData(jsonData));

        var result = sut.LoadAllFilesAtPaths<CustomFormatData>(new[] {Fs.CurrentDirectory()});

        result.Should().BeEquivalentTo(new[]
        {
            new CustomFormatData
            {
                Id = 22,
                Name = "FUNi",
                IncludeCustomFormatWhenRenaming = true
            }
        });
    }
}
