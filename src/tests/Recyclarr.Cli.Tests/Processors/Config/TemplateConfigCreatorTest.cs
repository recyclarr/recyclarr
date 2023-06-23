using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Processors.Config;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.ExceptionTypes;
using Recyclarr.TrashLib.Repo;

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
    public void Throw_when_file_exists_and_not_forced(
        [Frozen] IConfigTemplateGuideService templates,
        MockFileSystem fs,
        ICreateConfigSettings settings,
        TemplateConfigCreator sut)
    {
        templates.LoadTemplateData().Returns(new[]
        {
            new TemplatePath
            {
                Id = "template1",
                TemplateFile = fs.CurrentDirectory().File("template-file1.yml"),
                Service = SupportedServices.Radarr
            }
        });

        settings.Force.Returns(false);
        settings.Templates.Returns(new[]
        {
            "template1"
        });

        var act = () => sut.Create(settings);

        act.Should().ThrowAsync<FileExistsException>();
    }

    [Test, AutoMockData]
    public void No_throw_when_file_exists_and_forced(
        [Frozen] IConfigTemplateGuideService templates,
        MockFileSystem fs,
        ICreateConfigSettings settings,
        TemplateConfigCreator sut)
    {
        templates.LoadTemplateData().Returns(new[]
        {
            new TemplatePath
            {
                Id = "template1",
                TemplateFile = fs.CurrentDirectory().File("template-file1.yml"),
                Service = SupportedServices.Radarr
            }
        });

        settings.Force.Returns(true);
        settings.Templates.Returns(new[]
        {
            "template1"
        });

        var act = () => sut.Create(settings);

        act.Should().NotThrowAsync();
    }

    [Test]
    public void Template_id_matching_works()
    {
        const string templatesJson = @"
{
  'radarr': [
    {
      'template': 'template-file1.yml',
      'id': 'template1'
    }
  ],
  'sonarr': [
    {
      'template': 'template-file2.yml',
      'id': 'template2'
    },
    {
      'template': 'template-file3.yml',
      'id': 'template3'
    }
  ]
}";

        var repo = Resolve<IConfigTemplatesRepo>();
        Fs.AddFile(repo.Path.File("templates.json"), new MockFileData(templatesJson));
        Fs.AddEmptyFile(repo.Path.File("template-file1.yml"));
        Fs.AddEmptyFile(repo.Path.File("template-file2.yml"));
        // This one shouldn't show up in the result because the user didn't ask for it
        Fs.AddEmptyFile(repo.Path.File("template-file3.yml"));

        var settings = Substitute.For<ICreateConfigSettings>();
        settings.Templates.Returns(new[]
        {
            "template1",
            "template2",
            // This one shouldn't show up in the results because:
            // User specified it, but no template file exists for it.
            "template4"
        });

        var sut = Resolve<TemplateConfigCreator>();
        sut.Create(settings);

        Fs.AllFiles.Should().Contain(new[]
        {
            Paths.ConfigsDirectory.File("template-file1.yml").FullName,
            Paths.ConfigsDirectory.File("template-file2.yml").FullName
        });
    }
}
