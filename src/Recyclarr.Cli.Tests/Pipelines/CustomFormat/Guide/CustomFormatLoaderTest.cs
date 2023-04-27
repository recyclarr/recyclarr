using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using Newtonsoft.Json.Linq;
using Recyclarr.Cli.Pipelines.CustomFormat.Guide;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.TrashLib.TestLibrary;

namespace Recyclarr.Cli.Tests.Pipelines.CustomFormat.Guide;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CustomFormatLoaderTest : CliIntegrationFixture
{
    [Test]
    public void Get_custom_format_json_works()
    {
        var sut = Resolve<ICustomFormatLoader>();
        Fs.AddFile("first.json", new MockFileData("{'name':'first','trash_id':'1'}"));
        Fs.AddFile("second.json", new MockFileData("{'name':'second','trash_id':'2'}"));
        Fs.AddFile("collection_of_cfs.md", new MockFileData(""));

        var dir = Fs.CurrentDirectory();
        var results = sut.LoadAllCustomFormatsAtPaths(new[] {dir}, dir.File("collection_of_cfs.md"));

        results.Should().BeEquivalentTo(new[]
        {
            NewCf.Data("first", "1") with {FileName = "first.json"},
            NewCf.Data("second", "2") with {FileName = "second.json"}
        }, o => o.Excluding(x => x.Type == typeof(JObject)));
    }
}
