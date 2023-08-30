using System.IO.Abstractions;
using Recyclarr.Common.TestLibrary;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Repo;
using Recyclarr.TrashLib.TestLibrary;

namespace Recyclarr.TrashLib.Tests.Config.Services;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigTemplateGuideServiceTest : TrashLibIntegrationFixture
{
    [Test, AutoMockData]
    public void Throw_when_templates_dir_does_not_exist(
        ConfigTemplateGuideService sut)
    {
        var act = () => _ = sut.LoadTemplateData();

        act.Should().Throw<InvalidDataException>().WithMessage("Recyclarr*templates*");
    }

    [Test]
    public void Normal_behavior()
    {
        var repo = Resolve<IConfigTemplatesRepo>();
        var templateDir = repo.Path;
        Fs.AddSameFileFromEmbeddedResource(templateDir.File("templates.json"), typeof(ConfigTemplateGuideServiceTest));

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

        var data = sut.LoadTemplateData();
        data.Should().BeEquivalentTo(expectedPaths, o => o.Excluding(x => x.TemplateFile));
        data.Select(x => x.TemplateFile.FullName)
            .Should().BeEquivalentTo(expectedPaths.Select(x => x.TemplateFile.FullName));
    }
}
