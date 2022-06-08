using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using System.IO.Abstractions.TestingHelpers;
using AutoFixture.NUnit3;
using FluentAssertions;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using TestLibrary;
using TestLibrary.AutoFixture;
using TrashLib.Sonarr.ReleaseProfile;
using TrashLib.Sonarr.ReleaseProfile.Guide;

namespace TrashLib.Tests.Sonarr.ReleaseProfile.Guide;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class LocalRepoReleaseProfileJsonParserTest
{
    [Test, AutoMockData]
    public void Get_custom_format_json_works(
        [Frozen] IAppPaths paths,
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fileSystem,
        LocalRepoReleaseProfileJsonParser sut)
    {
        static ReleaseProfileData MakeMockObject(string term) => new()
        {
            Name = "name",
            TrashId = "123",
            Required = new TermData[]
            {
                new() {Term = term}
            }
        };

        static MockFileData MockFileData(dynamic obj) =>
            new MockFileData(JsonConvert.SerializeObject(obj));

        var mockData1 = MakeMockObject("first");
        var mockData2 = MakeMockObject("second");

        paths.RepoDirectory.Returns("");
        fileSystem.AddFile("docs/json/sonarr/first.json", MockFileData(mockData1));
        fileSystem.AddFile("docs/json/sonarr/second.json", MockFileData(mockData2));

        var results = sut.GetReleaseProfileData();

        results.Should().BeEquivalentTo(new[]
        {
            mockData1,
            mockData2
        });
    }

    [Test, AutoMockData]
    public void Json_exceptions_do_not_interrupt_parsing_other_files(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths paths,
        LocalRepoReleaseProfileJsonParser sut)
    {
        paths.RepoDirectory.Returns("");
        var rootPath = fs.CurrentDirectory()
            .SubDirectory("docs")
            .SubDirectory("json")
            .SubDirectory("sonarr");

        var badData = "# comment";
        var goodData = new ReleaseProfileData
        {
            Name = "name",
            TrashId = "123",
            Required = new TermData[]
            {
                new() {Term = "abc"}
            }
        };

        fs.AddFile(rootPath.File("0_bad_data.json").FullName, MockData.FromString(badData));
        fs.AddFile(rootPath.File("1_good_data.json").FullName, MockData.FromJson(goodData));

        var results = sut.GetReleaseProfileData();

        results.Should().BeEquivalentTo(new[] {goodData});
    }
}
