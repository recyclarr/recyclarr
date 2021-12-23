using AutoFixture.NUnit3;
using Common;
using FluentAssertions;
using LibGit2Sharp;
using NSubstitute;
using NUnit.Framework;
using TestLibrary.AutoFixture;
using TestLibrary.NSubstitute;
using VersionControl.Wrappers;

namespace VersionControl.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class GitRepositoryFactoryTest
{
    [Test, AutoMockData]
    public void Delete_and_clone_when_repo_is_not_valid(
        [Frozen] IRepositoryStaticWrapper wrapper,
        [Frozen] IFileUtilities fileUtils,
        GitRepositoryFactory sut)
    {
        wrapper.IsValid(Arg.Any<string>()).Returns(false);

        sut.CreateAndCloneIfNeeded("repo_url", "repo_path", "branch");

        Received.InOrder(() =>
        {
            wrapper.IsValid("repo_path");
            fileUtils.DeleteReadOnlyDirectory("repo_path");
            wrapper.Clone("repo_url", "repo_path",
                Verify.That<CloneOptions>(x => x.BranchName.Should().Be("branch")));
        });
    }

    [Test, AutoMockData]
    public void No_delete_and_clone_when_repo_is_valid(
        [Frozen] IRepositoryStaticWrapper wrapper,
        [Frozen] IFileUtilities fileUtils,
        GitRepositoryFactory sut)
    {
        wrapper.IsValid(Arg.Any<string>()).Returns(true);

        sut.CreateAndCloneIfNeeded("repo_url", "repo_path", "branch");

        wrapper.Received().IsValid("repo_path");
        fileUtils.DidNotReceiveWithAnyArgs().DeleteReadOnlyDirectory(default!);
        wrapper.DidNotReceiveWithAnyArgs().Clone(default!, default!, default!);
    }
}
