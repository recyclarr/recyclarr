using System.IO.Abstractions;
using Recyclarr.Settings;

namespace Recyclarr.Cli.IntegrationTests;

[TestFixture]
internal class ServiceCompatibilityIntegrationTest : CliIntegrationFixture
{
    [Test]
    public void Load_settings_yml_correctly_when_file_exists()
    {
        var sut = Resolve<SettingsProvider>();
        // For this test, it doesn't really matter if the YAML data matches what SettingsValue expects.
        // This test only ensures that the data deserialized is from the actual correct file.
        const string yamlData =
            """
            repositories:
              trash_guides:
                clone_url: http://the_url.com
            """;

        Fs.AddFile(Paths.AppDataDirectory.File("settings.yml"), new MockFileData(yamlData));

        var settings = sut.Settings;

        settings.Repositories.TrashGuides.CloneUrl.Should().Be("http://the_url.com");
    }
}
