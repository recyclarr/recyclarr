using Recyclarr.Cli.Migration.Steps;
using Recyclarr.Platform;

namespace Recyclarr.Cli.Tests.Migration.Steps;

internal sealed class MoveCacheToStateMigrationStepTest
{
    [Test, AutoMockData]
    public void CheckIfNeeded_returns_true_when_cache_directory_exists(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths
    )
    {
        var appDataDir = fs.DirectoryInfo.New("/app");
        appPaths.ConfigDirectory.Returns(appDataDir);
        fs.AddDirectory("/app/cache");

        var sut = new MoveCacheToStateMigrationStep(appPaths);

        var result = sut.CheckIfNeeded();

        result.Should().BeTrue();
    }

    [Test, AutoMockData]
    public void CheckIfNeeded_returns_false_when_cache_directory_does_not_exist(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths
    )
    {
        var appDataDir = fs.DirectoryInfo.New("/app");
        appPaths.ConfigDirectory.Returns(appDataDir);

        var sut = new MoveCacheToStateMigrationStep(appPaths);

        var result = sut.CheckIfNeeded();

        result.Should().BeFalse();
    }

    [Test, AutoMockData]
    public void Execute_moves_sonarr_cache_files_with_rename(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths,
        ILogger log
    )
    {
        var appDataDir = fs.DirectoryInfo.New("/app");
        appPaths.ConfigDirectory.Returns(appDataDir);

        fs.AddFile("/app/cache/sonarr/abc123/custom-format-cache.json", new MockFileData("{}"));
        fs.AddFile("/app/cache/sonarr/abc123/quality-profile-cache.json", new MockFileData("{}"));

        var sut = new MoveCacheToStateMigrationStep(appPaths);

        sut.Execute(log);

        fs.FileExists("/app/state/sonarr/abc123/custom-format-mappings.json").Should().BeTrue();
        fs.FileExists("/app/state/sonarr/abc123/quality-profile-mappings.json").Should().BeTrue();
        fs.FileExists("/app/cache/sonarr/abc123/custom-format-cache.json").Should().BeFalse();
        fs.FileExists("/app/cache/sonarr/abc123/quality-profile-cache.json").Should().BeFalse();
    }

    [Test, AutoMockData]
    public void Execute_moves_radarr_cache_files_with_rename(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths,
        ILogger log
    )
    {
        var appDataDir = fs.DirectoryInfo.New("/app");
        appPaths.ConfigDirectory.Returns(appDataDir);

        fs.AddFile("/app/cache/radarr/def456/custom-format-cache.json", new MockFileData("{}"));

        var sut = new MoveCacheToStateMigrationStep(appPaths);

        sut.Execute(log);

        fs.FileExists("/app/state/radarr/def456/custom-format-mappings.json").Should().BeTrue();
        fs.FileExists("/app/cache/radarr/def456/custom-format-cache.json").Should().BeFalse();
    }

    [Test, AutoMockData]
    public void Execute_skips_nonexistent_service_directory(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths,
        ILogger log
    )
    {
        var appDataDir = fs.DirectoryInfo.New("/app");
        appPaths.ConfigDirectory.Returns(appDataDir);

        fs.AddDirectory("/app/cache");

        var sut = new MoveCacheToStateMigrationStep(appPaths);

        // Should not throw
        var act = () => sut.Execute(log);

        act.Should().NotThrow();
        fs.Directory.Exists("/app/state/sonarr").Should().BeFalse();
        fs.Directory.Exists("/app/state/radarr").Should().BeFalse();
    }

    [Test, AutoMockData]
    public void Execute_creates_target_directory_structure(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths,
        ILogger log
    )
    {
        var appDataDir = fs.DirectoryInfo.New("/app");
        appPaths.ConfigDirectory.Returns(appDataDir);

        fs.AddFile("/app/cache/sonarr/hash1/test-cache.json", new MockFileData("{}"));

        var sut = new MoveCacheToStateMigrationStep(appPaths);

        sut.Execute(log);

        fs.Directory.Exists("/app/state/sonarr/hash1").Should().BeTrue();
    }

    [Test, AutoMockData]
    public void Execute_deletes_empty_source_hash_directory(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths,
        ILogger log
    )
    {
        var appDataDir = fs.DirectoryInfo.New("/app");
        appPaths.ConfigDirectory.Returns(appDataDir);

        fs.AddFile("/app/cache/sonarr/hash1/test-cache.json", new MockFileData("{}"));

        var sut = new MoveCacheToStateMigrationStep(appPaths);

        sut.Execute(log);

        fs.Directory.Exists("/app/cache/sonarr/hash1").Should().BeFalse();
    }

    [Test, AutoMockData]
    public void Execute_deletes_empty_source_service_directory(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths,
        ILogger log
    )
    {
        var appDataDir = fs.DirectoryInfo.New("/app");
        appPaths.ConfigDirectory.Returns(appDataDir);

        fs.AddFile("/app/cache/sonarr/hash1/test-cache.json", new MockFileData("{}"));

        var sut = new MoveCacheToStateMigrationStep(appPaths);

        sut.Execute(log);

        fs.Directory.Exists("/app/cache/sonarr").Should().BeFalse();
    }

    [Test, AutoMockData]
    public void Execute_moves_resources_directory_when_target_does_not_exist(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths,
        ILogger log
    )
    {
        var appDataDir = fs.DirectoryInfo.New("/app");
        appPaths.ConfigDirectory.Returns(appDataDir);
        appPaths.ResourceDirectory.Returns(fs.DirectoryInfo.New("/app/resources"));

        fs.AddFile("/app/cache/resources/test.json", new MockFileData("{}"));

        var sut = new MoveCacheToStateMigrationStep(appPaths);

        sut.Execute(log);

        fs.FileExists("/app/resources/test.json").Should().BeTrue();
        fs.Directory.Exists("/app/cache/resources").Should().BeFalse();
    }

    [Test, AutoMockData]
    public void Execute_deletes_source_resources_when_target_already_exists(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths,
        ILogger log
    )
    {
        var appDataDir = fs.DirectoryInfo.New("/app");
        appPaths.ConfigDirectory.Returns(appDataDir);

        fs.AddFile("/app/cache/resources/old.json", new MockFileData("{}"));
        fs.AddFile("/app/resources/new.json", new MockFileData("{}"));
        appPaths.ResourceDirectory.Returns(fs.DirectoryInfo.New("/app/resources"));

        var sut = new MoveCacheToStateMigrationStep(appPaths);

        sut.Execute(log);

        fs.Directory.Exists("/app/cache/resources").Should().BeFalse();
        fs.FileExists("/app/resources/new.json").Should().BeTrue();
    }

    [Test, AutoMockData]
    public void Execute_skips_resources_when_source_does_not_exist(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths,
        ILogger log
    )
    {
        var appDataDir = fs.DirectoryInfo.New("/app");
        appPaths.ConfigDirectory.Returns(appDataDir);
        appPaths.ResourceDirectory.Returns(fs.DirectoryInfo.New("/app/resources"));

        fs.AddDirectory("/app/cache");

        var sut = new MoveCacheToStateMigrationStep(appPaths);

        // Should not throw
        var act = () => sut.Execute(log);

        act.Should().NotThrow();
        fs.Directory.Exists("/app/resources").Should().BeFalse();
    }

    [Test, AutoMockData]
    public void Execute_deletes_empty_cache_directory_after_migration(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths,
        ILogger log
    )
    {
        var appDataDir = fs.DirectoryInfo.New("/app");
        appPaths.ConfigDirectory.Returns(appDataDir);

        fs.AddFile("/app/cache/sonarr/hash1/test-cache.json", new MockFileData("{}"));

        var sut = new MoveCacheToStateMigrationStep(appPaths);

        sut.Execute(log);

        fs.Directory.Exists("/app/cache").Should().BeFalse();
    }

    [Test, AutoMockData]
    public void Execute_keeps_cache_directory_when_not_empty(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths,
        ILogger log
    )
    {
        var appDataDir = fs.DirectoryInfo.New("/app");
        appPaths.ConfigDirectory.Returns(appDataDir);

        fs.AddFile("/app/cache/sonarr/hash1/test-cache.json", new MockFileData("{}"));
        fs.AddFile("/app/cache/unexpected-file.txt", new MockFileData("keep me"));

        var sut = new MoveCacheToStateMigrationStep(appPaths);

        sut.Execute(log);

        fs.Directory.Exists("/app/cache").Should().BeTrue();
        fs.FileExists("/app/cache/unexpected-file.txt").Should().BeTrue();
    }

    [Test, AutoMockData]
    public void Description_is_correct([Frozen] IAppPaths appPaths)
    {
        var sut = new MoveCacheToStateMigrationStep(appPaths);

        sut.Description.Should().Be("Move cache directory to state");
    }

    [Test, AutoMockData]
    public void Remediation_contains_expected_instructions(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths
    )
    {
        var appDataDir = fs.DirectoryInfo.New("/app");
        appPaths.ConfigDirectory.Returns(appDataDir);

        var sut = new MoveCacheToStateMigrationStep(appPaths);

        sut.Remediation.Should().HaveCount(4);
        sut.Remediation.Should().Contain(x => x.Contains("permission"));
        sut.Remediation.Should().Contain(x => x.Contains("sonarr") && x.Contains("radarr"));
        sut.Remediation.Should().Contain(x => x.Contains("resources"));
        sut.Remediation.Should().Contain(x => x.Contains("Delete"));
    }

    [Test, AutoMockData]
    public void Execute_handles_multiple_hash_directories(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths,
        ILogger log
    )
    {
        var appDataDir = fs.DirectoryInfo.New("/app");
        appPaths.ConfigDirectory.Returns(appDataDir);

        fs.AddFile("/app/cache/sonarr/hash1/cf-cache.json", new MockFileData("{}"));
        fs.AddFile("/app/cache/sonarr/hash2/qp-cache.json", new MockFileData("{}"));
        fs.AddFile("/app/cache/radarr/hash3/cf-cache.json", new MockFileData("{}"));

        var sut = new MoveCacheToStateMigrationStep(appPaths);

        sut.Execute(log);

        fs.FileExists("/app/state/sonarr/hash1/cf-mappings.json").Should().BeTrue();
        fs.FileExists("/app/state/sonarr/hash2/qp-mappings.json").Should().BeTrue();
        fs.FileExists("/app/state/radarr/hash3/cf-mappings.json").Should().BeTrue();
        fs.Directory.Exists("/app/cache").Should().BeFalse();
    }

    [Test, AutoMockData]
    public void Execute_moves_all_json_files_and_renames_only_cache_files(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths,
        ILogger log
    )
    {
        var appDataDir = fs.DirectoryInfo.New("/app");
        appPaths.ConfigDirectory.Returns(appDataDir);

        fs.AddFile("/app/cache/sonarr/hash1/test-cache.json", new MockFileData("{}"));
        fs.AddFile("/app/cache/sonarr/hash1/no-rename.json", new MockFileData("{}"));

        var sut = new MoveCacheToStateMigrationStep(appPaths);

        sut.Execute(log);

        // File with -cache.json should be renamed to -mappings.json
        fs.FileExists("/app/state/sonarr/hash1/test-mappings.json").Should().BeTrue();

        // File without -cache.json should be moved as-is without renaming
        fs.FileExists("/app/state/sonarr/hash1/no-rename.json").Should().BeTrue();
        fs.FileExists("/app/cache/sonarr/hash1/no-rename.json").Should().BeFalse();

        // Cache directory should be deleted since all files were moved
        fs.Directory.Exists("/app/cache").Should().BeFalse();
    }
}
