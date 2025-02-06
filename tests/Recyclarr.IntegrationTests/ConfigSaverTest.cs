using System.IO.Abstractions;
using Recyclarr.Config.Parsing;
using Recyclarr.TestLibrary;

namespace Recyclarr.IntegrationTests;

[TestFixture]
public class ConfigSaverTest : IntegrationTestFixture
{
    [Test]
    public void Replace_file_when_already_exists()
    {
        var sut = Resolve<ConfigSaver>();
        var config = new RootConfigYaml
        {
            Radarr = new Dictionary<string, RadarrConfigYaml?>
            {
                {
                    "instance1",
                    new RadarrConfigYaml { ApiKey = "apikey" }
                },
            },
        };

        var destFile = Fs.CurrentDirectory().File("config.yml");
        Fs.AddEmptyFile(destFile);

        sut.Save(config, destFile);

        Fs.GetFile(destFile).TextContents.Should().Contain("apikey");
    }

    [Test]
    public void Create_intermediate_directories()
    {
        var sut = Resolve<ConfigSaver>();

        var config = new RootConfigYaml
        {
            Radarr = new Dictionary<string, RadarrConfigYaml?>
            {
                {
                    "instance1",
                    new RadarrConfigYaml { ApiKey = "apikey", BaseUrl = "http://baseurl.com" }
                },
            },
        };

        var destFile = Fs.CurrentDirectory().SubDirectory("one", "two", "three").File("config.yml");

        sut.Save(config, destFile);

        const string expectedYaml = """
            radarr:
              instance1:
                api_key: apikey
                base_url: http://baseurl.com

            """;

        var expectedFile = Fs.GetFile(destFile);
        expectedFile.Should().NotBeNull();
        expectedFile.TextContents.Should().Be(expectedYaml);
    }
}
