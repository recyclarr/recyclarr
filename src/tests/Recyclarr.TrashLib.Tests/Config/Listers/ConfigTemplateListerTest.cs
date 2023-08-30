using System.IO.Abstractions;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Config.Listers;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.TestLibrary;
using Spectre.Console.Testing;

namespace Recyclarr.TrashLib.Tests.Config.Listers;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigTemplateListerTest : TrashLibIntegrationFixture
{
    [Test, AutoMockData]
    public void Hidden_templates_are_not_rendered(
        IFileInfo stubFile,
        [Frozen(Matching.ImplementedInterfaces)] TestConsole console,
        [Frozen] IConfigTemplateGuideService guideService,
        ConfigTemplateLister sut)
    {
        guideService.LoadTemplateData().Returns(new[]
        {
            new TemplatePath {Id = "r1", TemplateFile = stubFile, Service = SupportedServices.Radarr, Hidden = false},
            new TemplatePath {Id = "r2", TemplateFile = stubFile, Service = SupportedServices.Radarr, Hidden = false},
            new TemplatePath {Id = "s1", TemplateFile = stubFile, Service = SupportedServices.Sonarr, Hidden = false},
            new TemplatePath {Id = "s2", TemplateFile = stubFile, Service = SupportedServices.Sonarr, Hidden = true}
        });

        sut.List();

        console.Output.Should().NotContain("s2");
    }
}
