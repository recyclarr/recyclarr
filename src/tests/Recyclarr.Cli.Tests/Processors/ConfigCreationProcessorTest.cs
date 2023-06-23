using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using Autofac.Extras.Ordering;
using AutoFixture;
using Recyclarr.Cli.Console.Commands;
using Recyclarr.Cli.Processors.Config;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.TrashLib.ExceptionTypes;

namespace Recyclarr.Cli.Tests.Processors;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigCreationProcessorTest : CliIntegrationFixture
{
    [Test]
    public async Task Config_file_created_when_using_default_path()
    {
        var sut = Resolve<ConfigCreationProcessor>();

        await sut.Process(new ConfigCreateCommand.CliSettings
        {
            Path = null
        });

        var file = Fs.GetFile(Paths.AppDataDirectory.File("recyclarr.yml"));
        file.Should().NotBeNull();
        file.Contents.Should().NotBeEmpty();
    }

    [Test]
    public async Task Config_file_created_when_using_user_specified_path()
    {
        var sut = Resolve<ConfigCreationProcessor>();

        var settings = new ConfigCreateCommand.CliSettings
        {
            Path = Fs.CurrentDirectory()
                .SubDirectory("user")
                .SubDirectory("specified")
                .File("file.yml")
                .FullName
        };

        await sut.Process(settings);

        var file = Fs.GetFile(settings.Path);
        file.Should().NotBeNull();
        file.Contents.Should().NotBeEmpty();
    }

    [Test]
    public async Task Should_throw_if_file_already_exists()
    {
        var sut = Resolve<ConfigCreationProcessor>();

        var settings = new ConfigCreateCommand.CliSettings
        {
            Path = Fs.CurrentDirectory().File("file.yml").FullName
        };

        Fs.AddEmptyFile(settings.Path);

        var act = () => sut.Process(settings);

        await act.Should().ThrowAsync<FileExistsException>();
    }

    [SuppressMessage("Performance", "CA1812", Justification =
        "Used implicitly by test methods in this class")]
    private sealed class EmptyOrderedEnumerable : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Inject(Array.Empty<IConfigCreator>().AsOrdered());
        }
    }

    [Test, AutoMockData]
    public async Task Throw_when_no_config_creators_can_handle(
        [CustomizeWith(typeof(EmptyOrderedEnumerable))] ConfigCreationProcessor sut)
    {
        var settings = new ConfigCreateCommand.CliSettings();

        var act = () => sut.Process(settings);

        await act.Should().ThrowAsync<FatalException>();
    }
}
