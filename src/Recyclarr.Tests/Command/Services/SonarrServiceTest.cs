using AutoFixture.NUnit3;
using CliFx.Exceptions;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Recyclarr.Command;
using Recyclarr.Command.Services;
using TestLibrary.AutoFixture;
using TrashLib.Sonarr;

namespace Recyclarr.Tests.Command.Services;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class SonarrServiceTest
{
    [Test, AutoMockData]
    public async Task List_terms_without_value_fails(
        [Frozen] ISonarrCommand cmd,
        SonarrService sut)
    {
        cmd.ListReleaseProfiles.Returns(false);

        // When `--list-terms` is specified on the command line without a value, it gets a `null` value assigned.
        cmd.ListTerms.Returns((string?) null);

        var act = () => sut.Execute(cmd);

        await act.Should().ThrowAsync<CommandException>();
    }

    [Test, AutoMockData]
    public async Task List_terms_with_empty_value_fails(
        [Frozen] ISonarrCommand cmd,
        SonarrService sut)
    {
        cmd.ListReleaseProfiles.Returns(false);

        // If the user specifies a blank string as the value, it should still fail.
        cmd.ListTerms.Returns("");

        var act = () => sut.Execute(cmd);

        await act.Should().ThrowAsync<CommandException>();
    }

    [Test, AutoMockData]
    public async Task List_terms_uses_specified_trash_id(
        [Frozen] IReleaseProfileLister lister,
        [Frozen] ISonarrCommand cmd,
        SonarrService sut)
    {
        cmd.ListReleaseProfiles.Returns(false);

        cmd.ListTerms.Returns("some_id");

        await sut.Execute(cmd);

        lister.Received().ListTerms("some_id");
    }

    [Test, AutoMockData]
    public async Task List_release_profiles_is_invoked(
        [Frozen] IReleaseProfileLister lister,
        [Frozen] ISonarrCommand cmd,
        SonarrService sut)
    {
        cmd.ListReleaseProfiles.Returns(true);
        cmd.ListTerms.Returns((string?) null);

        await sut.Execute(cmd);

        lister.Received().ListReleaseProfiles();
    }
}
