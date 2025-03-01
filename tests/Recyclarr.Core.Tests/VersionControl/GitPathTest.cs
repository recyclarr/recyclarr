using Recyclarr.Repo;
using Recyclarr.Settings;

namespace Recyclarr.Core.Tests.VersionControl;

[TestFixture]
public class GitPathTest
{
    [Test, AutoMockData]
    public void Default_path_used_when_setting_is_null(
        [Frozen] ISettings<RecyclarrSettings> settings,
        GitPath sut
    )
    {
        settings.Value.Returns(new RecyclarrSettings { GitPath = null });

        var result = sut.Path;

        result.Should().Be(GitPath.Default);
    }

    [Test, AutoMockData]
    public void User_specified_path_used_instead_of_default(
        [Frozen] ISettings<RecyclarrSettings> settings,
        GitPath sut
    )
    {
        var expectedPath = "/usr/local/bin/git";
        settings.Value.Returns(new RecyclarrSettings { GitPath = expectedPath });

        var result = sut.Path;

        result.Should().Be(expectedPath);
    }
}
