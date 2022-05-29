using System.IO.Abstractions.TestingHelpers;
using System.Text.RegularExpressions;
using AutoFixture.NUnit3;
using FluentAssertions;
using NUnit.Framework;
using Recyclarr.Migration.Steps;
using TestLibrary;
using TestLibrary.AutoFixture;

namespace Recyclarr.Tests.Migration.Steps;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class MigrateTrashUpdaterAppDataDirTest
{
    private const string BasePath = "base_path";

    [Test, AutoMockData]
    public void Migration_check_returns_true_if_trash_updater_dir_exists(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen(Matching.ImplementedInterfaces)] TestAppPaths paths,
        MigrateTrashUpdaterAppDataDir sut)
    {
        fs.AddDirectory(fs.Path.Combine(paths.BasePath, "trash-updater"));
        sut.CheckIfNeeded().Should().BeTrue();
    }

    [Test, AutoMockData]
    public void Migration_check_returns_false_if_trash_updater_dir_doesnt_exists(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        MigrateTrashUpdaterAppDataDir sut)
    {
        sut.CheckIfNeeded().Should().BeFalse();
    }

    [Test, AutoMockData]
    public void Migration_throws_if_recyclarr_dir_already_exists(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        MigrateTrashUpdaterAppDataDir sut)
    {
        fs.AddDirectory(Path.Combine(BasePath, "trash-updater"));
        fs.AddDirectory(Path.Combine(BasePath, "recyclarr"));

        var act = () => sut.Execute(null);

        act.Should().Throw<IOException>();
    }

    [Test, AutoMockData]
    public void Migration_success(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen(Matching.ImplementedInterfaces)] TestAppPaths paths,
        MigrateTrashUpdaterAppDataDir sut)
    {
        // Add file instead of directory since the migration step only operates on files
        fs.AddFileNoData($"{paths.BasePath}/trash-updater/settings.yml");
        fs.AddFileNoData($"{paths.BasePath}/trash-updater/recyclarr.yml");
        fs.AddFileNoData($"{paths.BasePath}/trash-updater/this-gets-ignored.yml");
        fs.AddDirectory2($"{paths.BasePath}/trash-updater/repo");
        fs.AddDirectory2($"{paths.BasePath}/trash-updater/cache");
        fs.AddFileNoData($"{paths.BasePath}/trash-updater/cache/sonarr/test.txt");

        sut.Execute(null);

        fs.AllDirectories.Should().NotContain(x => x.Contains("trash-updater"));
        fs.AllFiles.Should().BeEquivalentTo(
            FileUtils.NormalizePath($"/{paths.BasePath}/recyclarr/settings.yml"),
            FileUtils.NormalizePath($"/{paths.BasePath}/recyclarr/recyclarr.yml"),
            FileUtils.NormalizePath($"/{paths.BasePath}/recyclarr/cache/sonarr/test.txt"));
    }
}
