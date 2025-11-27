using System.IO.Abstractions;
using System.Text.Json;
using Recyclarr.Core.TestLibrary;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Core.Tests.IntegrationTests;

internal sealed class CustomFormatLoaderIntegrationTest : IntegrationTestFixture
{
    [Test]
    public void Load_custom_format_json_works()
    {
        var sut = Resolve<JsonResourceLoader>();
        Fs.AddFile("first.json", new MockFileData("""{"name":"first","trash_id":"1"}"""));
        Fs.AddFile("second.json", new MockFileData("""{"name":"second","trash_id":"2"}"""));

        var files = new[]
        {
            Fs.FileInfo.New(Fs.Path.Combine(Fs.CurrentDirectory().FullName, "first.json")),
            Fs.FileInfo.New(Fs.Path.Combine(Fs.CurrentDirectory().FullName, "second.json")),
        };

        var results = sut.Load<CustomFormatResource>(files).Select(t => t.Resource).ToList();

        results
            .Should()
            .BeEquivalentTo(
                [NewCf.Data("first", "1"), NewCf.Data("second", "2")],
                o => o.Excluding(x => x.Type == typeof(JsonElement))
            );
    }
}
