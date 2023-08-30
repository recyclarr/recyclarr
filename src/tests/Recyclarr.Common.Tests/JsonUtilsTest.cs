using System.IO.Abstractions;
using Recyclarr.TestLibrary;

namespace Recyclarr.Common.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class JsonUtilsTest
{
    [Test]
    public void Log_files_that_do_not_exist()
    {
        var fs = new MockFileSystem();
        var log = new TestableLogger();

        var path = fs.CurrentDirectory().SubDirectory("doesnt_exist");

        var result = JsonUtils.GetJsonFilesInDirectories(new[] {path}, log);

        result.Should().BeEmpty();
        log.Messages.Should().ContainSingle()
            .Which.Should().Match("*doesnt_exist*");
    }

    [Test]
    public void Log_files_that_only_exist()
    {
        var fs = new MockFileSystem();
        var log = new TestableLogger();

        var path = fs.CurrentDirectory().SubDirectory("exists").File("test.json");
        fs.AddFile(path.FullName, new MockFileData(""));

        var result = JsonUtils.GetJsonFilesInDirectories(new[] {path.Directory}, log);

        result.Should().ContainSingle()
            .Which.FullName
            .Should().Be(path.FullName);

        log.Messages.Should().BeEmpty();
    }

    [Test]
    public void Log_files_that_both_exist_and_do_not_exist()
    {
        var fs = new MockFileSystem();
        var log = new TestableLogger();
        var paths = new[]
        {
            fs.CurrentDirectory().SubDirectory("does_not_exist"),
            fs.CurrentDirectory().SubDirectory("exists")
        };

        var existingFile = paths[1].File("test.json").FullName;

        fs.AddFile(existingFile, new MockFileData(""));
        paths[1].Refresh();

        var result = JsonUtils.GetJsonFilesInDirectories(paths, log);

        result.Should().ContainSingle()
            .Which.FullName
            .Should().Be(existingFile);

        log.Messages.Should().ContainSingle()
            .Which.Should().Match("*does_not_exist*");
    }

    [Test]
    public void Null_paths_are_ignored()
    {
        var result = JsonUtils.GetJsonFilesInDirectories(
            new IDirectoryInfo?[] {null, null},
            Substitute.For<ILogger>());

        result.Should().BeEmpty();
    }
}
