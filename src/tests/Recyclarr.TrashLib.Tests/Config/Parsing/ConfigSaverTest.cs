using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Config.Parsing;
using Recyclarr.TrashLib.TestLibrary;

namespace Recyclarr.TrashLib.Tests.Config.Parsing;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigSaverTest : TrashLibIntegrationFixture
{
    [Test]
    public void Replace_file_when_already_exists()
    {
        var sut = Resolve<ConfigSaver>();
        var config = new RootConfigYaml
        {
            Radarr = new Dictionary<string, RadarrConfigYaml>
            {
                {
                    "instance1", new RadarrConfigYaml
                    {
                        ApiKey = "apikey"
                    }
                }
            }
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
            Radarr = new Dictionary<string, RadarrConfigYaml>
            {
                {
                    "instance1", new RadarrConfigYaml
                    {
                        ApiKey = "apikey",
                        BaseUrl = "http://baseurl.com"
                    }
                }
            }
        };

        var destFile = Fs.CurrentDirectory().SubDir("one", "two", "three").File("config.yml");

        sut.Save(config, destFile);

        const string expectedYaml =
            """
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
