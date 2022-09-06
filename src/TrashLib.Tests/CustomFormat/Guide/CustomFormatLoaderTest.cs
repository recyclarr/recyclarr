using System.IO.Abstractions.Extensions;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Recyclarr.TestLibrary;
using TestLibrary.FluentAssertions;
using TrashLib.Services.CustomFormat.Guide;
using TrashLib.TestLibrary;

namespace TrashLib.Tests.CustomFormat.Guide;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CustomFormatLoaderTest : IntegrationFixture
{
    [Test]
    public void Get_custom_format_json_works()
    {
        var sut = Resolve<ICustomFormatLoader>();
        Fs.AddFile("first.json", new MockFileData("{'name':'first','trash_id':'1'}"));
        Fs.AddFile("second.json", new MockFileData("{'name':'second','trash_id':'2'}"));

        var results = sut.LoadAllCustomFormatsAtPaths(new[] {Fs.CurrentDirectory()});

        results.Should().BeEquivalentTo(new[]
        {
            NewCf.Data("first", "1"),
            NewCf.Data("second", "2")
        });
    }

    [Test]
    public void Trash_properties_are_removed()
    {
        Fs.AddFile("first.json", new MockFileData(@"
{
  'name':'first',
  'trash_id':'1',
  'trash_foo': 'foo',
  'trash_bar': 'bar',
  'extra': 'e1'
}"));

        var sut = Resolve<ICustomFormatLoader>();

        var results = sut.LoadAllCustomFormatsAtPaths(new[] {Fs.CurrentDirectory()});

        const string expectedExtraJson = @"{'name':'first','extra': 'e1'}";

        results.Should()
            .ContainSingle().Which.ExtraJson.Should()
            .BeEquivalentTo(JObject.Parse(expectedExtraJson), op => op.Using(new JsonEquivalencyStep()));
    }
}
