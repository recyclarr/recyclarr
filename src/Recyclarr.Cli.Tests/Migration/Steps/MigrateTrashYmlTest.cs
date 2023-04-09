using System.Text.RegularExpressions;
using Recyclarr.Cli.Migration.Steps;
using Recyclarr.TestLibrary.AutoFixture;

namespace Recyclarr.Cli.Tests.Migration.Steps;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public partial class MigrateTrashYmlTest
{
    private static readonly string BasePath = AppContext.BaseDirectory;

    [Test, AutoMockData]
    public void Migration_check_returns_true_if_trash_yml_exists(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        MigrateTrashYml sut)
    {
        fs.AddFile(Path.Combine(BasePath, "trash.yml"), new MockFileData(""));
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
        fs.AddFile(Path.Combine(BasePath, "recyclarr.yml"), new MockFileData(""));

        var act = () => sut.Execute(null);

        act.Should().Throw<IOException>();
    }

    [Test, AutoMockData]
    public void Migration_success(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        MigrateTrashYml sut)
    {
        const string expectedData = "fake contents";
        fs.AddFile(Path.Combine(BasePath, "trash.yml"), expectedData);

        sut.Execute(null);

        fs.AllFiles.Should().ContainSingle(x => RecyclarrYmlRegex().IsMatch(x));
    }

    [GeneratedRegex("[/\\\\]recyclarr\\.yml$")]
    private static partial Regex RecyclarrYmlRegex();
}
