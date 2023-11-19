using System.IO.Abstractions;
using System.Text.Json;
using Autofac;
using Recyclarr.TestLibrary.Autofac;
using Recyclarr.Tests.TestLibrary;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.IntegrationTests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CustomFormatLoaderIntegrationTest : IntegrationTestFixture
{
    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        base.RegisterStubsAndMocks(builder);
        builder.RegisterMockFor<ICustomFormatCategoryParser>();
    }

    [Test]
    public void Get_custom_format_json_works()
    {
        var sut = Resolve<CustomFormatLoader>();
        Fs.AddFile("first.json", new MockFileData("""{"name":"first","trash_id":"1"}"""));
        Fs.AddFile("second.json", new MockFileData("""{"name":"second","trash_id":"2"}"""));
        Fs.AddFile("collection_of_cfs.md", new MockFileData(""));

        var dir = Fs.CurrentDirectory();
        var results = sut.LoadAllCustomFormatsAtPaths(new[] {dir}, dir.File("collection_of_cfs.md"));

        results.Should().BeEquivalentTo(new[]
        {
            NewCf.Data("first", "1"),
            NewCf.Data("second", "2")
        }, o => o.Excluding(x => x.Type == typeof(JsonElement)));
    }

    [Test]
    public void Categorize_by_file_name()
    {
        var categoryParser = Resolve<ICustomFormatCategoryParser>();
        categoryParser.Parse(default!).ReturnsForAnyArgs(new[]
        {
            new CustomFormatCategoryItem("Streaming Services", "iTunes", "iT")
        });

        Fs.AddFile("it.json", new MockFileData("""{"name":"iT"}"""));
        Fs.AddEmptyFile("collection_of_cfs.md");

        var sut = Resolve<CustomFormatLoader>();

        var dir = Fs.CurrentDirectory();
        var results = sut.LoadAllCustomFormatsAtPaths(new[] {dir}, dir.File("collection_of_cfs.md"));

        results.Should().ContainSingle().Which.Category.Should().Be("Streaming Services");
    }
}
