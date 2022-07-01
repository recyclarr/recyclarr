using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using System.IO.Abstractions.TestingHelpers;
using AutoFixture.NUnit3;
using CliFx.Infrastructure;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Recyclarr.Command;
using TestLibrary.AutoFixture;
using TrashLib.TestLibrary;

// ReSharper disable MethodHasAsyncOverload

namespace Recyclarr.Tests.Command;

[TestFixture]
// Cannot be parallelized due to static CompositionRoot property
public class CreateConfigCommandTest
{
    [Test, AutoMockData]
    public async Task Config_file_created_when_using_default_path(
        [Frozen(Matching.ImplementedInterfaces)] TestAppPaths paths,
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        IServiceLocatorProxy container,
        ICompositionRoot compositionRoot,
        CreateConfigCommand sut)
    {
        BaseCommand.CompositionRoot = compositionRoot;

        await sut.Process(container);

        var file = fs.GetFile(paths.ConfigPath.FullName);
        file.Should().NotBeNull();
        file.Contents.Should().NotBeEmpty();
    }

    [Test, AutoMockData]
    public async Task Config_file_created_when_using_user_specified_path(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen(Matching.ImplementedInterfaces)] TestAppPaths paths,
        ICompositionRoot compositionRoot,
        CreateConfigCommand sut)
    {
        BaseCommand.CompositionRoot = compositionRoot;

        var ymlPath = fs.CurrentDirectory()
            .SubDirectory("user")
            .SubDirectory("specified")
            .File("file.yml").FullName;

        sut.AppDataDirectory = ymlPath;
        await sut.ExecuteAsync(Substitute.For<IConsole>());

        var file = fs.GetFile(ymlPath);
        file.Should().NotBeNull();
        file.Contents.Should().NotBeEmpty();
    }
}
