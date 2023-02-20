using Recyclarr.Cli.TestLibrary;
using Recyclarr.TrashLib.Config.Settings;

namespace Recyclarr.Cli.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ServiceCompatibilityIntegrationTest : IntegrationFixture
{
    [Test]
    public void Load_settings_yml_correctly_when_file_exists()
    {
        var sut = Resolve<ISettingsProvider>();

        // For this test, it doesn't really matter if the YAML data matches what SettingsValue expects.
        // This test only ensures that the data deserialized is from the actual correct file.
        const string yamlData = @"
repository:
  clone_url: http://the_url.com
";

        Fs.AddFile(Paths.SettingsPath.FullName, new MockFileData(yamlData));

        var settings = sut.Settings;

        settings.Repository.CloneUrl.Should().Be("http://the_url.com");
    }
}
