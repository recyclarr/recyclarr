using System.IO.Abstractions;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Processors.Config;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Startup;

namespace Recyclarr.Cli.Tests.Processors.Config;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class TemplateConfigCreatorTest : CliIntegrationFixture
{
    [Test, AutoMockData]
    public void Can_handle_returns_true_with_templates(
        ICreateConfigSettings settings,
        TemplateConfigCreator sut)
    {
        settings.Templates.Returns(new[] {"template1"});
        var result = sut.CanHandle(settings);
        result.Should().Be(true);
    }

    [Test, AutoMockData]
    public void Can_handle_returns_false_with_no_templates(
        ICreateConfigSettings settings,
        TemplateConfigCreator sut)
    {
        settings.Templates.Returns(Array.Empty<string>());
        var result = sut.CanHandle(settings);
        result.Should().Be(false);
    }

    [Test, AutoMockData]
    public async Task No_replace_when_file_exists_and_not_forced(
        [Frozen] IConfigTemplateGuideService templates,
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths paths,
        ICreateConfigSettings settings,
        TemplateConfigCreator sut)
    {
        var templateFile = fs.CurrentDirectory().File("template-file1.yml");
        var destFile = paths.ConfigsDirectory.File(templateFile.Name);

        fs.AddFile(templateFile, new MockFileData("a"));
        fs.AddFile(destFile, new MockFileData("b"));

        settings.Force.Returns(false);
        settings.Path.Returns(templateFile.FullName);

        await sut.Create(settings);

        fs.GetFile(destFile).TextContents.Should().Be("b");
    }

    [Test, AutoMockData]
    public async Task No_throw_when_file_exists_and_forced(
        [Frozen] IConfigTemplateGuideService templates,
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths paths,
        ICreateConfigSettings settings,
        TemplateConfigCreator sut)
    {
        var templateFile = fs.CurrentDirectory().File("template-file1.yml");
        fs.AddEmptyFile(templateFile);
        fs.AddEmptyFile(paths.ConfigsDirectory.File(templateFile.Name));

        settings.Force.Returns(true);
        settings.Path.Returns(templateFile.FullName);

        var act = () => sut.Create(settings);

        await act.Should().NotThrowAsync();
    }
}
