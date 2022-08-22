using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AutoFixture.NUnit3;
using FluentAssertions;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using TestLibrary;
using TestLibrary.AutoFixture;
using TrashLib.Repo;
using TrashLib.Services.Sonarr.ReleaseProfile;
using TrashLib.Services.Sonarr.ReleaseProfile.Guide;
using TrashLib.Startup;

namespace TrashLib.Tests.Sonarr.ReleaseProfile.Guide;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class LocalRepoReleaseProfileJsonParserTest
{
    [Test, AutoMockData]
    public void Get_custom_format_json_works(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths,
        [Frozen] IRepoPaths repoPaths,
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

        var baseDir = appPaths.RepoDirectory
            .SubDirectory("docs")
            .SubDirectory("json")
            .SubDirectory("sonarr");

        fs.AddFile(baseDir.File("first.json").FullName, MockFileData(mockData1));
        fs.AddFile(baseDir.File("second.json").FullName, MockFileData(mockData2));

        repoPaths.SonarrReleaseProfilePaths.Returns(new[] {baseDir});

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
        [Frozen] IAppPaths appPaths,
        [Frozen] IRepoPaths repoPaths,
        LocalRepoReleaseProfileJsonParser sut)
    {
        var rootPath = appPaths.RepoDirectory
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

        repoPaths.SonarrReleaseProfilePaths.Returns(new[] {rootPath});

        var results = sut.GetReleaseProfileData();

        results.Should().BeEquivalentTo(new[] {goodData});
    }
}
