using System.IO.Abstractions;
using Recyclarr.Repo;
using Recyclarr.TestLibrary;
using Recyclarr.TestLibrary.AutoFixture;
using Recyclarr.TrashGuide;

namespace Recyclarr.IntegrationTests;

[TestFixture]
public class ConfigTemplateGuideServiceIntegrationTest : IntegrationTestFixture
{
    [Test, AutoMockData]
    public void Throw_when_templates_dir_does_not_exist(
        ConfigTemplateGuideService sut)
    {
        var act = () => _ = sut.GetTemplateData();

        act.Should().Throw<InvalidDataException>().WithMessage("Recyclarr*templates*");
    }

    [Test]
    public void Normal_behavior()
    {
        var repo = Resolve<IConfigTemplatesRepo>();
        var templateDir = repo.Path;
        Fs.AddSameFileFromEmbeddedResource(templateDir.File("templates.json"),
            typeof(ConfigTemplateGuideServiceIntegrationTest));

        TemplatePath MakeTemplatePath(SupportedServices service, string id, string path)
        {
            var fsPath = templateDir.File(path);
            Fs.AddEmptyFile(fsPath);
            return new TemplatePath {Service = service, Id = id, TemplateFile = fsPath, Hidden = false};
        }

        var expectedPaths = new[]
        {
            MakeTemplatePath(SupportedServices.Radarr, "hd-bluray-web", "radarr/hd-bluray-web.yml"),
            MakeTemplatePath(SupportedServices.Radarr, "uhd-bluray-web", "radarr/uhd-bluray-web.yml"),
            MakeTemplatePath(SupportedServices.Sonarr, "web-1080p-v4", "sonarr/web-1080p-v4.yml")
        };

        var sut = Resolve<ConfigTemplateGuideService>();

        var data = sut.GetTemplateData();
        data.Should().BeEquivalentTo(expectedPaths, o => o.Excluding(x => x.TemplateFile));
        data.Select(x => x.TemplateFile.FullName)
            .Should().BeEquivalentTo(expectedPaths.Select(x => x.TemplateFile.FullName));
    }
}
