using System.IO.Abstractions.TestingHelpers;
using AutoFixture.NUnit3;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using TestLibrary.AutoFixture;
using TrashLib.Radarr.CustomFormat.Guide;

namespace TrashLib.Tests.Radarr.CustomFormat.Guide;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class LocalRepoCustomFormatJsonParserTest
{
    [Test, AutoMockData]
    public void Get_custom_format_json_works(
        [Frozen] IAppPaths paths,
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fileSystem,
        LocalRepoCustomFormatJsonParser sut)
    {
        paths.RepoDirectory.Returns("");
        fileSystem.AddFile("docs/json/radarr/first.json", new MockFileData("first"));
        fileSystem.AddFile("docs/json/radarr/second.json", new MockFileData("second"));

        var results = sut.GetCustomFormatJson();

        results.Should().BeEquivalentTo("first", "second");
    }
}
