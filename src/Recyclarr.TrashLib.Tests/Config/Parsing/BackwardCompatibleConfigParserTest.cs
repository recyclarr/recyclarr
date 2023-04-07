using Recyclarr.Cli.TestLibrary;
using Recyclarr.TrashLib.Config.Parsing;

namespace Recyclarr.TrashLib.Tests.Config.Parsing;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class BackwardCompatibleConfigParserTest : IntegrationFixture
{
    [Test]
    public void Load_v1_into_latest()
    {
        var sut = Resolve<BackwardCompatibleConfigParser>();

        var yaml = @"
sonarr:
  - api_key: key1
    base_url: url1
  - api_key: key2
    base_url: url2
radarr:
  - api_key: key3
    base_url: url3
";
        var result = sut.ParseYamlConfig(() => new StringReader(yaml));

        result.Should().NotBeNull();
        result!.Radarr!.Keys.Concat(result.Sonarr!.Keys)
            .Should().BeEquivalentTo("instance1", "instance2", "instance3");

        result.Sonarr.Values.Should().BeEquivalentTo(new[]
        {
            new
            {
                BaseUrl = "url1",
                ApiKey = "key1"
            },
            new
            {
                BaseUrl = "url2",
                ApiKey = "key2"
            }
        });

        result.Radarr.Values.Should().BeEquivalentTo(new[]
        {
            new
            {
                BaseUrl = "url3",
                ApiKey = "key3"
            }
        });
    }

    [Test]
    public void Load_v2_into_latest()
    {
        var sut = Resolve<BackwardCompatibleConfigParser>();

        var yaml = @"
sonarr:
  instance1:
    api_key: key1
    base_url: url1
  instance2:
    api_key: key2
    base_url: url2
radarr:
  instance3:
    api_key: key3
    base_url: url3
";
        var result = sut.ParseYamlConfig(() => new StringReader(yaml));

        result.Should().BeEquivalentTo(new RootConfigYamlLatest
        {
            Sonarr = new Dictionary<string, SonarrConfigYamlLatest>
            {
                {
                    "instance1", new SonarrConfigYamlLatest
                    {
                        BaseUrl = "url1",
                        ApiKey = "key1"
                    }
                },
                {
                    "instance2", new SonarrConfigYamlLatest
                    {
                        BaseUrl = "url2",
                        ApiKey = "key2"
                    }
                }
            },
            Radarr = new Dictionary<string, RadarrConfigYamlLatest>
            {
                {
                    "instance3", new RadarrConfigYamlLatest
                    {
                        BaseUrl = "url3",
                        ApiKey = "key3"
                    }
                }
            }
        });
    }
}
