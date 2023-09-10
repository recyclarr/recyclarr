using Recyclarr.TrashLib.Config.Parsing;
using Recyclarr.TrashLib.Config.Parsing.PostProcessing;
using Recyclarr.TrashLib.Config.Secrets;

namespace Recyclarr.TrashLib.Config.Tests.Parsing.PostProcessing;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ImplicitUrlAndKeyPostProcessorTest
{
    [Test, AutoMockData]
    public void Update_only_base_url_when_absent(
        [Frozen] ISecretsProvider secrets,
        ImplicitUrlAndKeyPostProcessor sut)
    {
        secrets.Secrets.Returns(new Dictionary<string, string>
        {
            {"instance1_base_url", "secret_base_url_1"},
            {"instance1_api_key", "secret_api_key_1"},
            {"instance2_base_url", "secret_base_url_2"},
            {"instance2_api_key", "secret_api_key_2"}
        });

        var result = sut.Process(new RootConfigYaml
        {
            Radarr = new Dictionary<string, RadarrConfigYaml>
            {
                {
                    "instance1", new RadarrConfigYaml
                    {
                        ApiKey = "explicit_base_url"
                    }
                }
            },
            Sonarr = new Dictionary<string, SonarrConfigYaml>
            {
                {
                    "instance2", new SonarrConfigYaml
                    {
                        ApiKey = "explicit_base_url"
                    }
                }
            }
        });

        result.Should().BeEquivalentTo(new RootConfigYaml
        {
            Radarr = new Dictionary<string, RadarrConfigYaml>
            {
                {
                    "instance1", new RadarrConfigYaml
                    {
                        ApiKey = "explicit_base_url",
                        BaseUrl = "secret_base_url_1"
                    }
                }
            },
            Sonarr = new Dictionary<string, SonarrConfigYaml>
            {
                {
                    "instance2", new SonarrConfigYaml
                    {
                        ApiKey = "explicit_base_url",
                        BaseUrl = "secret_base_url_2"
                    }
                }
            }
        });
    }

    [Test, AutoMockData]
    public void Update_only_api_key_when_absent(
        [Frozen] ISecretsProvider secrets,
        ImplicitUrlAndKeyPostProcessor sut)
    {
        secrets.Secrets.Returns(new Dictionary<string, string>
        {
            {"instance1_base_url", "secret_base_url_1"},
            {"instance1_api_key", "secret_api_key_1"},
            {"instance2_base_url", "secret_base_url_2"},
            {"instance2_api_key", "secret_api_key_2"}
        });

        var result = sut.Process(new RootConfigYaml
        {
            Radarr = new Dictionary<string, RadarrConfigYaml>
            {
                {
                    "instance1", new RadarrConfigYaml
                    {
                        BaseUrl = "explicit_base_url"
                    }
                }
            },
            Sonarr = new Dictionary<string, SonarrConfigYaml>
            {
                {
                    "instance2", new SonarrConfigYaml
                    {
                        BaseUrl = "explicit_base_url"
                    }
                }
            }
        });

        result.Should().BeEquivalentTo(new RootConfigYaml
        {
            Radarr = new Dictionary<string, RadarrConfigYaml>
            {
                {
                    "instance1", new RadarrConfigYaml
                    {
                        ApiKey = "secret_api_key_1",
                        BaseUrl = "explicit_base_url"
                    }
                }
            },
            Sonarr = new Dictionary<string, SonarrConfigYaml>
            {
                {
                    "instance2", new SonarrConfigYaml
                    {
                        ApiKey = "secret_api_key_2",
                        BaseUrl = "explicit_base_url"
                    }
                }
            }
        });
    }

    [Test, AutoMockData]
    public void Update_base_url_and_api_key_when_absent(
        [Frozen] ISecretsProvider secrets,
        ImplicitUrlAndKeyPostProcessor sut)
    {
        secrets.Secrets.Returns(new Dictionary<string, string>
        {
            {"instance1_base_url", "secret_base_url_1"},
            {"instance1_api_key", "secret_api_key_1"},
            {"instance2_base_url", "secret_base_url_2"},
            {"instance2_api_key", "secret_api_key_2"}
        });

        var result = sut.Process(new RootConfigYaml
        {
            Radarr = new Dictionary<string, RadarrConfigYaml>
            {
                {"instance1", new RadarrConfigYaml()}
            },
            Sonarr = new Dictionary<string, SonarrConfigYaml>
            {
                {"instance2", new SonarrConfigYaml()}
            }
        });

        result.Should().BeEquivalentTo(new RootConfigYaml
        {
            Radarr = new Dictionary<string, RadarrConfigYaml>
            {
                {
                    "instance1", new RadarrConfigYaml
                    {
                        ApiKey = "secret_api_key_1",
                        BaseUrl = "secret_base_url_1"
                    }
                }
            },
            Sonarr = new Dictionary<string, SonarrConfigYaml>
            {
                {
                    "instance2", new SonarrConfigYaml
                    {
                        ApiKey = "secret_api_key_2",
                        BaseUrl = "secret_base_url_2"
                    }
                }
            }
        });
    }

    [Test, AutoMockData]
    public void Change_nothing_when_all_explicitly_provided(
        [Frozen] ISecretsProvider secrets,
        ImplicitUrlAndKeyPostProcessor sut)
    {
        secrets.Secrets.Returns(new Dictionary<string, string>
        {
            {"instance1_base_url", "secret_base_url_1"},
            {"instance1_api_key", "secret_api_key_1"},
            {"instance2_base_url", "secret_base_url_2"},
            {"instance2_api_key", "secret_api_key_2"}
        });

        var result = sut.Process(new RootConfigYaml
        {
            Radarr = new Dictionary<string, RadarrConfigYaml>
            {
                {
                    "instance1", new RadarrConfigYaml
                    {
                        BaseUrl = "explicit_base_url",
                        ApiKey = "explicit_api_key"
                    }
                }
            },
            Sonarr = new Dictionary<string, SonarrConfigYaml>
            {
                {
                    "instance2", new SonarrConfigYaml
                    {
                        BaseUrl = "explicit_base_url",
                        ApiKey = "explicit_api_key"
                    }
                }
            }
        });

        result.Should().BeEquivalentTo(new RootConfigYaml
        {
            Radarr = new Dictionary<string, RadarrConfigYaml>
            {
                {
                    "instance1", new RadarrConfigYaml
                    {
                        ApiKey = "explicit_api_key",
                        BaseUrl = "explicit_base_url"
                    }
                }
            },
            Sonarr = new Dictionary<string, SonarrConfigYaml>
            {
                {
                    "instance2", new SonarrConfigYaml
                    {
                        ApiKey = "explicit_api_key",
                        BaseUrl = "explicit_base_url"
                    }
                }
            }
        });
    }
}
