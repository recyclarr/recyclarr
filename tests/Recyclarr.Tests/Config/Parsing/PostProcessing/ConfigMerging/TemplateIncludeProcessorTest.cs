using System.IO.Abstractions;
using Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;
using Recyclarr.TrashGuide;

namespace Recyclarr.Tests.Config.Parsing.PostProcessing.ConfigMerging;

[TestFixture]
public class TemplateIncludeProcessorTest
{
    [Test, AutoMockData]
    public void Can_process_expected_type(
        TemplateIncludeProcessor sut)
    {
        var result = sut.CanProcess(new TemplateYamlInclude());
        result.Should().BeTrue();
    }

    [Test, AutoMockData]
    public void Obtain_path_from_template(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IConfigTemplateGuideService templates,
        TemplateIncludeProcessor sut)
    {
        var templatePath = fs.CurrentDirectory().File("some/path/template.yml");
        templates.GetIncludeData().Returns(new[]
        {
            new TemplatePath
            {
                Id = "my-template",
                Service = SupportedServices.Radarr,
                TemplateFile = templatePath
            }
        });

        var includeDirective = new TemplateYamlInclude {Template = "my-template"};

        var path = sut.GetPathToConfig(includeDirective, SupportedServices.Radarr);

        path.FullName.Should().Be(templatePath.FullName);
    }

    [Test, AutoMockData]
    public void Throw_when_template_is_null(
        TemplateIncludeProcessor sut)
    {
        var includeDirective = new TemplateYamlInclude {Template = null};

        var act = () => sut.GetPathToConfig(includeDirective, SupportedServices.Radarr);

        act.Should().Throw<YamlIncludeException>().WithMessage("*template*is required*");
    }

    [Test, AutoMockData]
    public void Throw_when_service_types_are_mixed(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IConfigTemplateGuideService templates,
        TemplateIncludeProcessor sut)
    {
        var templatePath = fs.CurrentDirectory().File("some/path/template.yml");
        templates.GetIncludeData().Returns(new[]
        {
            new TemplatePath
            {
                Id = "my-template",
                Service = SupportedServices.Radarr,
                TemplateFile = templatePath
            }
        });

        var includeDirective = new TemplateYamlInclude {Template = "my-template"};

        var act = () => sut.GetPathToConfig(includeDirective, SupportedServices.Sonarr);

        act.Should().Throw<YamlIncludeException>().WithMessage("*unable to find*");
    }

    [Test, AutoMockData]
    public void Throw_when_no_template_found(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IConfigTemplateGuideService templates,
        TemplateIncludeProcessor sut)
    {
        var templatePath = fs.CurrentDirectory().File("some/path/template.yml");
        templates.GetIncludeData().Returns(new[]
        {
            new TemplatePath
            {
                Id = "my-template",
                Service = SupportedServices.Radarr,
                TemplateFile = templatePath
            }
        });

        var includeDirective = new TemplateYamlInclude {Template = "template-does-not-exist"};

        var act = () => sut.GetPathToConfig(includeDirective, SupportedServices.Radarr);

        act.Should().Throw<YamlIncludeException>().WithMessage("*unable to find*");
    }
}
