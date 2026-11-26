using Recyclarr.Migration.Steps;
using Recyclarr.Platform;

namespace Recyclarr.Core.Tests.Migration.Steps;

internal sealed class MoveCacheToStateMigrationStepTest
{
    private readonly MockFileSystem _fs = new();
    private readonly IAppPaths _appPaths = Substitute.For<IAppPaths>();
    private readonly ILogger _log = Substitute.For<ILogger>();

    [Test]
    public void Run_does_nothing_when_cache_directory_does_not_exist()
    {
        _appPaths.ConfigDirectory.Returns(_fs.DirectoryInfo.New("/app"));
        _appPaths.StateDirectory.Returns(_fs.DirectoryInfo.New("/app/state"));
        _appPaths.ResourceDirectory.Returns(_fs.DirectoryInfo.New("/app/resources"));

        var sut = new MoveCacheToStateMigrationStep(_appPaths);

        sut.Run(_log);

        _fs.Directory.Exists("/app/state").Should().BeFalse();
        _fs.Directory.Exists("/app/resources").Should().BeFalse();
    }

    [Test]
    public void Execute_moves_sonarr_cache_files_with_rename()
    {
        var appDataDir = _fs.DirectoryInfo.New("/app");
        _appPaths.ConfigDirectory.Returns(appDataDir);
        _appPaths.StateDirectory.Returns(_fs.DirectoryInfo.New("/app/state"));

        _fs.AddFile("/app/cache/sonarr/abc123/custom-format-cache.json", new MockFileData("{}"));
        _fs.AddFile("/app/cache/sonarr/abc123/quality-profile-cache.json", new MockFileData("{}"));

        var sut = new MoveCacheToStateMigrationStep(_appPaths);

        sut.Run(_log);

        _fs.FileExists("/app/state/sonarr/abc123/custom-format-mappings.json").Should().BeTrue();
        _fs.FileExists("/app/state/sonarr/abc123/quality-profile-mappings.json").Should().BeTrue();
        _fs.FileExists("/app/cache/sonarr/abc123/custom-format-cache.json").Should().BeFalse();
        _fs.FileExists("/app/cache/sonarr/abc123/quality-profile-cache.json").Should().BeFalse();
    }

    [Test]
    public void Execute_moves_radarr_cache_files_with_rename()
    {
        var appDataDir = _fs.DirectoryInfo.New("/app");
        _appPaths.ConfigDirectory.Returns(appDataDir);
        _appPaths.StateDirectory.Returns(_fs.DirectoryInfo.New("/app/state"));

        _fs.AddFile("/app/cache/radarr/def456/custom-format-cache.json", new MockFileData("{}"));

        var sut = new MoveCacheToStateMigrationStep(_appPaths);

        sut.Run(_log);

        _fs.FileExists("/app/state/radarr/def456/custom-format-mappings.json").Should().BeTrue();
        _fs.FileExists("/app/cache/radarr/def456/custom-format-cache.json").Should().BeFalse();
    }

    [Test]
    public void Execute_skips_nonexistent_service_directory()
    {
        var appDataDir = _fs.DirectoryInfo.New("/app");
        _appPaths.ConfigDirectory.Returns(appDataDir);
        _appPaths.StateDirectory.Returns(_fs.DirectoryInfo.New("/app/state"));

        _fs.AddDirectory("/app/cache");

        var sut = new MoveCacheToStateMigrationStep(_appPaths);

        // Should not throw
        var act = () => sut.Run(_log);

        act.Should().NotThrow();
        _fs.Directory.Exists("/app/state/sonarr").Should().BeFalse();
        _fs.Directory.Exists("/app/state/radarr").Should().BeFalse();
    }

    [Test]
    public void Execute_creates_target_directory_structure()
    {
        var appDataDir = _fs.DirectoryInfo.New("/app");
        _appPaths.ConfigDirectory.Returns(appDataDir);
        _appPaths.StateDirectory.Returns(_fs.DirectoryInfo.New("/app/state"));

        _fs.AddFile("/app/cache/sonarr/hash1/test-cache.json", new MockFileData("{}"));

        var sut = new MoveCacheToStateMigrationStep(_appPaths);

        sut.Run(_log);

        _fs.Directory.Exists("/app/state/sonarr/hash1").Should().BeTrue();
    }

    [Test]
    public void Execute_deletes_empty_source_hash_directory()
    {
        var appDataDir = _fs.DirectoryInfo.New("/app");
        _appPaths.ConfigDirectory.Returns(appDataDir);
        _appPaths.StateDirectory.Returns(_fs.DirectoryInfo.New("/app/state"));

        _fs.AddFile("/app/cache/sonarr/hash1/test-cache.json", new MockFileData("{}"));

        var sut = new MoveCacheToStateMigrationStep(_appPaths);

        sut.Run(_log);

        _fs.Directory.Exists("/app/cache/sonarr/hash1").Should().BeFalse();
    }

    [Test]
    public void Execute_deletes_empty_source_service_directory()
    {
        var appDataDir = _fs.DirectoryInfo.New("/app");
        _appPaths.ConfigDirectory.Returns(appDataDir);
        _appPaths.StateDirectory.Returns(_fs.DirectoryInfo.New("/app/state"));

        _fs.AddFile("/app/cache/sonarr/hash1/test-cache.json", new MockFileData("{}"));

        var sut = new MoveCacheToStateMigrationStep(_appPaths);

        sut.Run(_log);

        _fs.Directory.Exists("/app/cache/sonarr").Should().BeFalse();
    }

    [Test]
    public void Execute_moves_resources_directory_when_target_does_not_exist()
    {
        var appDataDir = _fs.DirectoryInfo.New("/app");
        _appPaths.ConfigDirectory.Returns(appDataDir);
        _appPaths.StateDirectory.Returns(_fs.DirectoryInfo.New("/app/state"));
        _appPaths.ResourceDirectory.Returns(_fs.DirectoryInfo.New("/app/resources"));

        _fs.AddFile("/app/cache/resources/test.json", new MockFileData("{}"));

        var sut = new MoveCacheToStateMigrationStep(_appPaths);

        sut.Run(_log);

        _fs.FileExists("/app/resources/test.json").Should().BeTrue();
        _fs.Directory.Exists("/app/cache/resources").Should().BeFalse();
    }

    [Test]
    public void Execute_deletes_source_resources_when_target_already_exists()
    {
        var appDataDir = _fs.DirectoryInfo.New("/app");
        _appPaths.ConfigDirectory.Returns(appDataDir);
        _appPaths.StateDirectory.Returns(_fs.DirectoryInfo.New("/app/state"));

        _fs.AddFile("/app/cache/resources/old.json", new MockFileData("{}"));
        _fs.AddFile("/app/resources/new.json", new MockFileData("{}"));
        _appPaths.ResourceDirectory.Returns(_fs.DirectoryInfo.New("/app/resources"));

        var sut = new MoveCacheToStateMigrationStep(_appPaths);

        sut.Run(_log);

        _fs.Directory.Exists("/app/cache/resources").Should().BeFalse();
        _fs.FileExists("/app/resources/new.json").Should().BeTrue();
    }

    [Test]
    public void Execute_skips_resources_when_source_does_not_exist()
    {
        var appDataDir = _fs.DirectoryInfo.New("/app");
        _appPaths.ConfigDirectory.Returns(appDataDir);
        _appPaths.StateDirectory.Returns(_fs.DirectoryInfo.New("/app/state"));
        _appPaths.ResourceDirectory.Returns(_fs.DirectoryInfo.New("/app/resources"));

        _fs.AddDirectory("/app/cache");

        var sut = new MoveCacheToStateMigrationStep(_appPaths);

        // Should not throw
        var act = () => sut.Run(_log);

        act.Should().NotThrow();
        _fs.Directory.Exists("/app/resources").Should().BeFalse();
    }

    [Test]
    public void Execute_deletes_empty_cache_directory_after_migration()
    {
        var appDataDir = _fs.DirectoryInfo.New("/app");
        _appPaths.ConfigDirectory.Returns(appDataDir);
        _appPaths.StateDirectory.Returns(_fs.DirectoryInfo.New("/app/state"));

        _fs.AddFile("/app/cache/sonarr/hash1/test-cache.json", new MockFileData("{}"));

        var sut = new MoveCacheToStateMigrationStep(_appPaths);

        sut.Run(_log);

        _fs.Directory.Exists("/app/cache").Should().BeFalse();
    }

    [Test]
    public void Execute_keeps_cache_directory_when_not_empty()
    {
        var appDataDir = _fs.DirectoryInfo.New("/app");
        _appPaths.ConfigDirectory.Returns(appDataDir);
        _appPaths.StateDirectory.Returns(_fs.DirectoryInfo.New("/app/state"));

        _fs.AddFile("/app/cache/sonarr/hash1/test-cache.json", new MockFileData("{}"));
        _fs.AddFile("/app/cache/unexpected-file.txt", new MockFileData("keep me"));

        var sut = new MoveCacheToStateMigrationStep(_appPaths);

        sut.Run(_log);

        _fs.Directory.Exists("/app/cache").Should().BeTrue();
        _fs.FileExists("/app/cache/unexpected-file.txt").Should().BeTrue();
    }

    [Test]
    public void Execute_handles_multiple_hash_directories()
    {
        var appDataDir = _fs.DirectoryInfo.New("/app");
        _appPaths.ConfigDirectory.Returns(appDataDir);
        _appPaths.StateDirectory.Returns(_fs.DirectoryInfo.New("/app/state"));

        _fs.AddFile("/app/cache/sonarr/hash1/cf-cache.json", new MockFileData("{}"));
        _fs.AddFile("/app/cache/sonarr/hash2/qp-cache.json", new MockFileData("{}"));
        _fs.AddFile("/app/cache/radarr/hash3/cf-cache.json", new MockFileData("{}"));

        var sut = new MoveCacheToStateMigrationStep(_appPaths);

        sut.Run(_log);

        _fs.FileExists("/app/state/sonarr/hash1/cf-mappings.json").Should().BeTrue();
        _fs.FileExists("/app/state/sonarr/hash2/qp-mappings.json").Should().BeTrue();
        _fs.FileExists("/app/state/radarr/hash3/cf-mappings.json").Should().BeTrue();
        _fs.Directory.Exists("/app/cache").Should().BeFalse();
    }

    [Test]
    public void Execute_moves_all_json_files_and_renames_only_cache_files()
    {
        var appDataDir = _fs.DirectoryInfo.New("/app");
        _appPaths.ConfigDirectory.Returns(appDataDir);
        _appPaths.StateDirectory.Returns(_fs.DirectoryInfo.New("/app/state"));

        _fs.AddFile("/app/cache/sonarr/hash1/test-cache.json", new MockFileData("{}"));
        _fs.AddFile("/app/cache/sonarr/hash1/no-rename.json", new MockFileData("{}"));

        var sut = new MoveCacheToStateMigrationStep(_appPaths);

        sut.Run(_log);

        // File with -cache.json should be renamed to -mappings.json
        _fs.FileExists("/app/state/sonarr/hash1/test-mappings.json").Should().BeTrue();

        // File without -cache.json should be moved as-is without renaming
        _fs.FileExists("/app/state/sonarr/hash1/no-rename.json").Should().BeTrue();
        _fs.FileExists("/app/cache/sonarr/hash1/no-rename.json").Should().BeFalse();

        // Cache directory should be deleted since all files were moved
        _fs.Directory.Exists("/app/cache").Should().BeFalse();
    }
}
