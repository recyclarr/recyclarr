using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AutoFixture.NUnit3;
using FluentAssertions;
using NUnit.Framework;
using Recyclarr.Cli.Migration.Steps;
using Recyclarr.TestLibrary;
using Recyclarr.TestLibrary.AutoFixture;
using Recyclarr.TrashLib.TestLibrary;

namespace Recyclarr.Cli.Tests.Migration.Steps;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class MigrateTrashUpdaterAppDataDirTest
{
    [Test, AutoMockData]
    public void Migration_check_returns_true_if_trash_updater_dir_exists(
        [Frozen(Matching.ImplementedInterfaces)] TestAppPaths paths,
        MigrateTrashUpdaterAppDataDir sut)
    {
        paths.AppDataDirectory.Parent.SubDirectory("trash-updater").Create();
        sut.CheckIfNeeded().Should().BeTrue();
    }

    [Test, AutoMockData]
    public void Migration_check_returns_false_if_trash_updater_dir_doesnt_exists(
        MigrateTrashUpdaterAppDataDir sut)
    {
        sut.CheckIfNeeded().Should().BeFalse();
    }

    [Test, AutoMockData]
    public void Migration_throws_if_recyclarr_yml_already_exists(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen(Matching.ImplementedInterfaces)] TestAppPaths paths,
        MigrateTrashUpdaterAppDataDir sut)
    {
        fs.AddFileNoData(sut.OldPath.File("recyclarr.yml"));
        fs.AddFileNoData(sut.NewPath.File("recyclarr.yml"));

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
        var baseDir = sut.OldPath;
        fs.AddFileNoData(baseDir.File("settings.yml"));
        fs.AddFileNoData(baseDir.File("recyclarr.yml"));
        fs.AddFileNoData(baseDir.File("this-gets-ignored.yml"));
        fs.AddDirectory(baseDir.SubDirectory("repo"));
        fs.AddDirectory(baseDir.SubDirectory("cache"));
        fs.AddFileNoData(baseDir.File("cache/sonarr/test.txt"));

        sut.Execute(null);

        var expectedBase = sut.NewPath;

        fs.AllDirectories.Should().NotContain(x => x.Contains("trash-updater"));
        fs.AllFiles.Should().BeEquivalentTo(
            expectedBase.File("settings.yml").FullName,
            expectedBase.File("recyclarr.yml").FullName,
            expectedBase.SubDirectory("cache").SubDirectory("sonarr").File("test.txt").FullName);
    }

    [Test, AutoMockData]
    public void No_exception_if_source_files_do_not_exist(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen(Matching.ImplementedInterfaces)] TestAppPaths paths,
        MigrateTrashUpdaterAppDataDir sut)
    {
        var act = () => sut.Execute(null);

        act.Should().NotThrow();
    }
}
