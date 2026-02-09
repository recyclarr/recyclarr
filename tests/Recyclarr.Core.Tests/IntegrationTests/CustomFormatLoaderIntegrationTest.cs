using System.IO.Abstractions;
using System.Text.Json;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Json;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Core.Tests.IntegrationTests;

[CoreDataSource]
internal sealed class CustomFormatLoaderIntegrationTest(JsonResourceLoader sut, MockFileSystem fs)
{
    [Test]
    public void Load_custom_format_json_works()
    {
        fs.AddFile("first.json", new MockFileData("""{"name":"first","trash_id":"1"}"""));
        fs.AddFile("second.json", new MockFileData("""{"name":"second","trash_id":"2"}"""));

        var files = new[]
        {
            fs.FileInfo.New(fs.Path.Combine(fs.CurrentDirectory().FullName, "first.json")),
            fs.FileInfo.New(fs.Path.Combine(fs.CurrentDirectory().FullName, "second.json")),
        };

        var results = sut.Load<CustomFormatResource>(files, GlobalJsonSerializerSettings.Guide)
            .Select(t => t.Resource)
            .ToList();

        results
            .Should()
            .BeEquivalentTo(
                [NewCf.Data("first", "1"), NewCf.Data("second", "2")],
                o => o.Excluding(x => x.Type == typeof(JsonElement))
            );
    }
}
