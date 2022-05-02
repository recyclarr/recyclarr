using AutoFixture.NUnit3;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using TestLibrary.AutoFixture;
using Recyclarr.Command;
using TrashLib.Sonarr;

namespace Recyclarr.Tests.Command;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class SonarrCommandTest
{
    [Test, AutoMockData]
    public async Task List_terms_without_value_fails(
        IConsole console,
        SonarrCommand sut)
    {
        // When `--list-terms` is specified on the command line without a value, it gets a `null` value assigned.
        sut.ListTerms = null;

        var act = () => sut.ExecuteAsync(console).AsTask();

        await act.Should().ThrowAsync<CommandException>();
    }

    [Test, AutoMockData]
    public async Task List_terms_with_empty_value_fails(
        IConsole console,
        SonarrCommand sut)
    {
        // If the user specifies a blank string as the value, it should still fail.
        sut.ListTerms = "";

        var act = () => sut.ExecuteAsync(console).AsTask();

        await act.Should().ThrowAsync<CommandException>();
    }

    [Test, AutoMockData]
    public async Task List_terms_uses_specified_trash_id(
        [Frozen] IReleaseProfileLister lister,
        IConsole console,
        SonarrCommand sut)
    {
        sut.ListTerms = "some_id";

        await sut.ExecuteAsync(console);

        lister.Received().ListTerms("some_id");
    }

    [Test, AutoMockData]
    public async Task List_release_profiles_is_invoked(
        [Frozen] IReleaseProfileLister lister,
        IConsole console,
        SonarrCommand sut)
    {
        sut.ListReleaseProfiles = true;

        await sut.ExecuteAsync(console);

        lister.Received().ListReleaseProfiles();
    }
}
