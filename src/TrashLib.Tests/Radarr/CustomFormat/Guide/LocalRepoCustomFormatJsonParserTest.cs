using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using System.IO.Abstractions.TestingHelpers;
using AutoFixture.NUnit3;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using TestLibrary.AutoFixture;
using TrashLib.Radarr.CustomFormat.Guide;
using TrashLib.TestLibrary;

namespace TrashLib.Tests.Radarr.CustomFormat.Guide;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class LocalRepoCustomFormatJsonParserTest
{
    [Test, AutoMockData]
    public void Get_custom_format_json_works(
        [Frozen] IAppPaths paths,
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        LocalRepoCustomFormatJsonParser sut)
    {
        var jsonDir = fs.CurrentDirectory()
            .SubDirectory("docs")
            .SubDirectory("json")
            .SubDirectory("radarr");

        paths.RepoDirectory.Returns(fs.CurrentDirectory().FullName);
        fs.AddFile(jsonDir.File("first.json").FullName, new MockFileData("{'name':'first','trash_id':'1'}"));
        fs.AddFile(jsonDir.File("second.json").FullName, new MockFileData("{'name':'second','trash_id':'2'}"));

        var results = sut.GetCustomFormatData();

        results.Should().BeEquivalentTo(new[]
        {
            NewCf.Data("first", "1"),
            NewCf.Data("second", "2")
        });
    }
}
