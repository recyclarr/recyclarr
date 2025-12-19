using System.IO.Abstractions;
using Recyclarr.Json;

namespace Recyclarr.Core.Tests.Json;

internal sealed class JsonUtilsTest
{
    [Test]
    public void Return_empty_when_directory_does_not_exist()
    {
        var fs = new MockFileSystem();
        var path = fs.CurrentDirectory().SubDirectory("doesnt_exist");

        var result = JsonUtils.GetJsonFilesInDirectories([path], Substitute.For<ILogger>());

        result.Should().BeEmpty();
    }

    [Test]
    public void Return_files_from_existing_directory()
    {
        var fs = new MockFileSystem();
        var path = fs.CurrentDirectory().SubDirectory("exists").File("test.json");
        fs.AddFile(path.FullName, new MockFileData(""));

        var result = JsonUtils.GetJsonFilesInDirectories(
            [path.Directory],
            Substitute.For<ILogger>()
        );

        result.Should().ContainSingle().Which.FullName.Should().Be(path.FullName);
    }

    [Test]
    public void Return_files_only_from_existing_directories()
    {
        var fs = new MockFileSystem();
        var paths = new[]
        {
            fs.CurrentDirectory().SubDirectory("does_not_exist"),
            fs.CurrentDirectory().SubDirectory("exists"),
        };

        var existingFile = paths[1].File("test.json").FullName;

        fs.AddFile(existingFile, new MockFileData(""));
        paths[1].Refresh();

        var result = JsonUtils.GetJsonFilesInDirectories(paths, Substitute.For<ILogger>());

        result.Should().ContainSingle().Which.FullName.Should().Be(existingFile);
    }

    [Test]
    public void Null_paths_are_ignored()
    {
        var result = JsonUtils.GetJsonFilesInDirectories([null, null], Substitute.For<ILogger>());

        result.Should().BeEmpty();
    }
}
