using System.IO.Abstractions.TestingHelpers;
using System.Text.RegularExpressions;
using AutoFixture.NUnit3;
using FluentAssertions;
using NUnit.Framework;
using Recyclarr.Migration.Steps;
using TestLibrary.AutoFixture;

namespace Recyclarr.Tests.Migration.Steps;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class MigrateTrashUpdaterAppDataDirTest
{
    private static readonly string BasePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    [Test, AutoMockData]
    public void Migration_check_returns_true_if_trash_updater_dir_exists(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        MigrateTrashUpdaterAppDataDir sut)
    {
        fs.AddDirectory(Path.Combine(BasePath, "trash-updater"));
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

        var act = () => sut.Execute();

        act.Should().Throw<IOException>();
    }

    [Test, AutoMockData]
    public void Migration_success(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        MigrateTrashUpdaterAppDataDir sut)
    {
        fs.AddDirectory(Path.Combine(BasePath, "trash-updater"));

        sut.Execute();

        fs.AllDirectories.Should().ContainSingle(x => Regex.IsMatch(x, @"[/\\]recyclarr$"));
    }
}
