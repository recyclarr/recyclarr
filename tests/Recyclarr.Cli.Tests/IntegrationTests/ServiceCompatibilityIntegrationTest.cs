using System.IO.Abstractions;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.Cli.Tests.IntegrationTests;

internal sealed class ServiceCompatibilityIntegrationTest : CliIntegrationFixture
{
    [Test]
    public void Load_settings_yml_correctly_when_file_exists()
    {
        const string yamlData = """
            repositories:
              trash_guides:
                clone_url: http://the_url.com
            """;

        Fs.AddFile(Paths.AppDataDirectory.File("settings.yml"), new MockFileData(yamlData));

        var settings = Resolve<ISettings<TrashRepository>>();

        settings.Value.CloneUrl.Should().Be("http://the_url.com");
    }
}
