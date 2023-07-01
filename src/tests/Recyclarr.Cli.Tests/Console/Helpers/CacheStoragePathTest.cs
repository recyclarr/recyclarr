using Recyclarr.Cli.Console.Helpers;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.Cli.Tests.Console.Helpers;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CacheStoragePathTest
{
    [Test, AutoMockData]
    public void Use_correct_name_in_path(CacheStoragePath sut)
    {
        var config = new SonarrConfiguration
        {
            BaseUrl = new Uri("http://something/foo/bar"),
            InstanceName = "thename"
        };

        var result = sut.CalculatePath(config, "obj");

        result.FullName.Should().MatchRegex(@".*[/\\][a-f0-9]+[/\\]obj\.json$");
    }

    [Test, AutoMockData]
    public void Migration_old_path_moved_to_new_path(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        CacheStoragePath sut)
    {
        var config = new SonarrConfiguration
        {
            BaseUrl = new Uri("http://something"),
            InstanceName = "thename"
        };

        var oldPath = sut.CalculateOldPath(config, "obj");
        var newPath = sut.CalculatePath(config, "obj");

        fs.AddEmptyFile(oldPath);

        sut.MigrateOldPath(config, "obj");

        fs.AllFiles.Should().Contain(newPath.FullName);
        fs.AllFiles.Should().NotContain(oldPath.FullName);
    }

    [Test, AutoMockData]
    public void Migration_old_path_deleted_when_new_path_already_exists(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        CacheStoragePath sut)
    {
        var config = new SonarrConfiguration
        {
            BaseUrl = new Uri("http://something"),
            InstanceName = "thename"
        };

        var oldPath = sut.CalculateOldPath(config, "obj");
        var newPath = sut.CalculatePath(config, "obj");

        fs.AddEmptyFile(oldPath);
        fs.AddFile(newPath, new MockFileData("something"));

        sut.MigrateOldPath(config, "obj");

        fs.AllFiles.Should().NotContain(oldPath.FullName);

        var file = fs.GetFile(newPath);
        file.Should().NotBeNull();
        file.TextContents.Should().Be("something");
    }

    [Test, AutoMockData]
    public void Migration_nothing_moved_if_old_path_not_exist(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        CacheStoragePath sut)
    {
        var config = new SonarrConfiguration
        {
            BaseUrl = new Uri("http://something"),
            InstanceName = "thename"
        };

        var oldPath = sut.CalculateOldPath(config, "obj");
        var newPath = sut.CalculatePath(config, "obj");

        sut.MigrateOldPath(config, "obj");

        fs.AllFiles.Should().NotContain(oldPath.FullName);
        fs.AllFiles.Should().NotContain(newPath.FullName);
    }
}
