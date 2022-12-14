using System.IO.Abstractions;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using FluentAssertions;
using NUnit.Framework;
using Recyclarr.Cli.Command;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.Common.TestLibrary;
using Recyclarr.TestLibrary.AutoFixture;
using Recyclarr.TrashLib.Repo;

namespace Recyclarr.Cli.Tests.Command;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class SonarrCommandTest : IntegrationFixture
{
    [Test, AutoMockData]
    public async Task List_terms_without_value_fails(
        IConsole console,
        SonarrCommand sut)
    {
        sut.ListReleaseProfiles = false;

        // When `--list-terms` is specified on the command line without a value, it gets a `null` value assigned.
        sut.ListTerms = null;

        var act = async () => await sut.Process(Container);

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

        var act = async () => await sut.Process(Container);

        await act.Should().ThrowAsync<CommandException>();
    }

    [Test]
    public async Task List_terms_uses_specified_trash_id()
    {
        var repoPaths = Resolve<IRepoPathsFactory>().Create();
        var cfDir = repoPaths.SonarrReleaseProfilePaths.First();
        Fs.AddFileFromResource(cfDir.File("optionals.json"), "optionals.json");

        var sut = new SonarrCommand
        {
            ListReleaseProfiles = false,
            ListTerms = "76e060895c5b8a765c310933da0a5357"
        };

        await sut.Process(Container);

        Console.ReadOutputString().Should().Contain("List of Terms");
    }

    [Test]
    public async Task List_release_profiles_is_invoked()
    {
        var sut = new SonarrCommand
        {
            ListReleaseProfiles = true,
            ListTerms = null
        };

        await sut.Process(Container);

        Console.ReadOutputString().Should().Contain("List of Release Profiles");
    }
}
