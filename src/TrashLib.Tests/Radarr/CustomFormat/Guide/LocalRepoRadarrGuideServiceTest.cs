using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AutoFixture.NUnit3;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using TestLibrary.AutoFixture;
using TrashLib.Repo;
using TrashLib.Services.Radarr.CustomFormat.Guide;
using TrashLib.Startup;
using TrashLib.TestLibrary;

namespace TrashLib.Tests.Radarr.CustomFormat.Guide;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class LocalRepoRadarrGuideServiceTest
{
    [Test, AutoMockData]
    public void Get_custom_format_json_works(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths,
        [Frozen] IRepoPaths repoPaths,
        LocalRepoRadarrGuideService sut)
    {
        var jsonDir = appPaths.RepoDirectory
            .SubDirectory("docs")
            .SubDirectory("json")
            .SubDirectory("radarr");

        fs.AddFile(jsonDir.File("first.json").FullName, new MockFileData("{'name':'first','trash_id':'1'}"));
        fs.AddFile(jsonDir.File("second.json").FullName, new MockFileData("{'name':'second','trash_id':'2'}"));

        repoPaths.RadarrCustomFormatPaths.Returns(new[] {jsonDir});

        var results = sut.GetCustomFormatData();

        results.Should().BeEquivalentTo(new[]
        {
            NewCf.Data("first", "1"),
            NewCf.Data("second", "2")
        });
    }
}
