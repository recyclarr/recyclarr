using System.IO.Abstractions.TestingHelpers;
using System.Text.RegularExpressions;
using AutoFixture.NUnit3;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Recyclarr.Migration;
using Recyclarr.Migration.Steps;
using Serilog;
using TestLibrary.AutoFixture;

namespace Recyclarr.Tests.Migration.Steps;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class MigrateTrashYmlTest
{
    private static readonly string BasePath = AppContext.BaseDirectory;

    [Test, AutoMockData]
    public void Migration_check_returns_true_if_trash_yml_exists(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        MigrateTrashYml sut)
    {
        fs.AddFile(Path.Combine(BasePath, "trash.yml"), MockFileData.NullObject);
        sut.CheckIfNeeded().Should().BeTrue();
    }

    [Test, AutoMockData]
    public void Migration_check_returns_false_if_trash_yml_doesnt_exists(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        MigrateTrashYml sut)
    {
        sut.CheckIfNeeded().Should().BeFalse();
    }

    [Test, AutoMockData]
    public void Migration_throws_if_recyclarr_yml_already_exists(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        MigrateTrashYml sut)
    {
        fs.AddFile(Path.Combine(BasePath, "recyclarr.yml"), MockFileData.NullObject);

        var act = () => sut.Execute(Substitute.For<ILogger>());

        act.Should().Throw<MigrationException>().WithMessage("*already exist*");
    }

    [Test, AutoMockData]
    public void Migration_success(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        MigrateTrashYml sut)
    {
        const string expectedData = "fake contents";
        fs.AddFile(Path.Combine(BasePath, "trash.yml"), expectedData);

        sut.Execute(Substitute.For<ILogger>());

        fs.AllFiles.Should().ContainSingle(x => Regex.IsMatch(x, @"[/\\]recyclarr\.yml$"));
    }
}
