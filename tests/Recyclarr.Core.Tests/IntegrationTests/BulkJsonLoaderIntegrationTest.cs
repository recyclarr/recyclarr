using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Json.Loading;

namespace Recyclarr.Core.Tests.IntegrationTests;

internal sealed class BulkJsonLoaderIntegrationTest : IntegrationTestFixture
{
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
    private sealed record TestGuideObject(string TrashId, int TrashScore, string Name);

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
    private sealed record TestServiceObject(
        int Id,
        string Name,
        bool IncludeCustomFormatWhenRenaming
    );

    [Test]
    public void Guide_deserialize_works()
    {
        var sut = Resolve<GuideJsonLoader>();

        const string jsonData = """
            {
              "trash_id": "90cedc1fea7ea5d11298bebd3d1d3223",
              "trash_score": "-10000",
              "name": "TheName"
            }
            """;

        Fs.AddFile(Fs.CurrentDirectory().File("file.json"), new MockFileData(jsonData));

        var result = sut.LoadAllFilesAtPaths<TestGuideObject>([Fs.CurrentDirectory()]);

        result
            .Should()
            .BeEquivalentTo([
                new TestGuideObject("90cedc1fea7ea5d11298bebd3d1d3223", -10000, "TheName"),
            ]);
    }

    [Test]
    public void Service_deserialize_works()
    {
        var sut = Resolve<ServiceJsonLoader>();

        const string jsonData = """
            {
              "id": 22,
              "name": "FUNi",
              "includeCustomFormatWhenRenaming": true
            }
            """;

        Fs.AddFile(Fs.CurrentDirectory().File("file.json"), new MockFileData(jsonData));

        var result = sut.LoadAllFilesAtPaths<TestServiceObject>([Fs.CurrentDirectory()]);

        result.Should().BeEquivalentTo([new TestServiceObject(22, "FUNi", true)]);
    }
}
