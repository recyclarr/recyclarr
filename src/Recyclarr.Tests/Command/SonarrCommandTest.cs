using AutoFixture.NUnit3;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Recyclarr.Command;
using TestLibrary.AutoFixture;
using TrashLib.Services.Sonarr;

namespace Recyclarr.Tests.Command;

[TestFixture]
// Cannot be parallelized due to static CompositionRoot property
public class SonarrCommandTest
{
    [Test, AutoMockData]
    public async Task List_terms_without_value_fails(
        IConsole console,
        SonarrCommand sut)
    {
        sut.ListReleaseProfiles = false;

        // When `--list-terms` is specified on the command line without a value, it gets a `null` value assigned.
        sut.ListTerms = null;

        var act = async () => await sut.ExecuteAsync(console);

        await act.Should().ThrowAsync<CommandException>();
    }

    [Test, AutoMockData]
    public async Task List_terms_with_empty_value_fails(
        IConsole console,
        SonarrCommand sut)
    {
        sut.ListReleaseProfiles = false;

        // If the user specifies a blank string as the value, it should still fail.
        sut.ListTerms = "";

        var act = async () => await sut.ExecuteAsync(console);

        await act.Should().ThrowAsync<CommandException>();
    }

    [Test, AutoMockData]
    public async Task List_terms_uses_specified_trash_id(
        [Frozen] ISonarrGuideDataLister lister,
        IConsole console,
        ICompositionRoot compositionRoot,
        SonarrCommand sut)
    {
        BaseCommand.CompositionRoot = compositionRoot;
        sut.ListReleaseProfiles = false;

        sut.ListTerms = "some_id";

        await sut.ExecuteAsync(console);

        lister.Received().ListTerms("some_id");
    }

    [Test, AutoMockData]
    public async Task List_release_profiles_is_invoked(
        [Frozen] ISonarrGuideDataLister lister,
        IConsole console,
        ICompositionRoot compositionRoot,
        SonarrCommand sut)
    {
        BaseCommand.CompositionRoot = compositionRoot;

        sut.ListReleaseProfiles = true;
        sut.ListTerms = null;

        await sut.ExecuteAsync(console);

        lister.Received().ListReleaseProfiles();
    }
}
