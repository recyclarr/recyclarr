using System.IO.Abstractions.TestingHelpers;
using AutoFixture.NUnit3;
using FluentAssertions;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
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
}
