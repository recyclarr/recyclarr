using Microsoft.Extensions.Options;
using Recyclarr.Repo;
using Recyclarr.Settings.Models;

namespace Recyclarr.Core.Tests.VersionControl;

internal sealed class GitPathTest
{
    [Test]
    public void Default_path_used_when_setting_is_null()
    {
        var sut = new GitPath(Options.Create(new RecyclarrSettings { GitPath = null }));

        var result = sut.Path;

        result.Should().Be(GitPath.Default);
    }

    [Test]
    public void User_specified_path_used_instead_of_default()
    {
        var expectedPath = "/usr/local/bin/git";
        var sut = new GitPath(Options.Create(new RecyclarrSettings { GitPath = expectedPath }));

        var result = sut.Path;

        result.Should().Be(expectedPath);
    }
}
