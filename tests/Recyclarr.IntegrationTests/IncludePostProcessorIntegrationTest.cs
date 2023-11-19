using System.IO.Abstractions;
using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.PostProcessing;
using Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

namespace Recyclarr.IntegrationTests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class IncludePostProcessorIntegrationTest : IntegrationTestFixture
{
    [Test]
    public void No_change_when_no_includes()
    {
        var sut = Resolve<IncludePostProcessor>();

        var config = new RootConfigYaml
        {
            Radarr = new Dictionary<string, RadarrConfigYaml>
            {
                ["service1"] = new()
                {
                    ApiKey = "asdf",
                    BaseUrl = "fdsa"
                }
            }
        };

        var result = sut.Process(config);

        result.Should().BeEquivalentTo(config);
    }

    [Test]
    public void Throw_when_unable_to_parse()
    {
        var sut = Resolve<IncludePostProcessor>();

        var configPath = Fs.CurrentDirectory().File("my-include.yml");

        Fs.AddFile(configPath, new MockFileData(
            """
            asdf: invalid
            """));

        var config = new RootConfigYaml
        {
            Radarr = new Dictionary<string, RadarrConfigYaml>
            {
                ["service1"] = new()
                {
                    Include = new[]
                    {
                        new ConfigYamlInclude {Config = configPath.FullName}
                    }
                }
            }
        };

        var act = () => sut.Process(config);

        act.Should().Throw<YamlIncludeException>().WithMessage("*parse include file*my-include.yml*");
    }

    [Test]
    public void Throw_when_unable_to_validate()
    {
        var sut = Resolve<IncludePostProcessor>();

        var configPath = Fs.CurrentDirectory().File("my-include.yml");

        Fs.AddFile(configPath, new MockFileData(
            """
            custom_formats:
            """));

        var config = new RootConfigYaml
        {
            Radarr = new Dictionary<string, RadarrConfigYaml>
            {
                ["service1"] = new()
                {
                    Include = new[]
                    {
                        new ConfigYamlInclude {Config = configPath.FullName}
                    }
                }
            }
        };

        var act = () => sut.Process(config);

        act.Should().Throw<YamlIncludeException>().WithMessage("*Validation*failed*my-include.yml*");
    }

    [Test]
    public void Merge_works()
    {
        var sut = Resolve<IncludePostProcessor>();

        var configPath1 = Fs.CurrentDirectory().File("my-include1.yml");
        Fs.AddFile(configPath1, new MockFileData(
            """
            custom_formats:
              - trash_ids:
                  - 496f355514737f7d83bf7aa4d24f8169

            quality_definition:
              type: anime
              preferred_ratio: 0.75

            delete_old_custom_formats: false
            """));

        var configPath2 = Fs.CurrentDirectory().File("sub_dir/my-include2.yml");
        Fs.AddFile(configPath2, new MockFileData(
            """
            custom_formats:
              - trash_ids:
                  - 240770601cc226190c367ef59aba7463

            delete_old_custom_formats: true
            """));

        var config = new RootConfigYaml
        {
            Radarr = new Dictionary<string, RadarrConfigYaml>
            {
                ["service1"] = new()
                {
                    BaseUrl = "the_base_url",
                    ApiKey = "the_api_key",
                    CustomFormats = new[]
                    {
                        new CustomFormatConfigYaml
                        {
                            TrashIds = new[] {"2f22d89048b01681dde8afe203bf2e95"}
                        }
                    },
                    QualityDefinition = new QualitySizeConfigYaml
                    {
                        Type = "series"
                    },
                    Include = new[]
                    {
                        new ConfigYamlInclude {Config = configPath1.FullName},
                        new ConfigYamlInclude {Config = configPath2.FullName}
                    }
                }
            }
        };

        var result = sut.Process(config);

        result.Should().BeEquivalentTo(new RootConfigYaml
        {
            Radarr = new Dictionary<string, RadarrConfigYaml>
            {
                ["service1"] = new()
                {
                    BaseUrl = "the_base_url",
                    ApiKey = "the_api_key",
                    Include = null,
                    CustomFormats = new[]
                    {
                        new CustomFormatConfigYaml
                        {
                            TrashIds = new[]
                            {
                                "496f355514737f7d83bf7aa4d24f8169",
                                "240770601cc226190c367ef59aba7463"
                            }
                        },
                        new CustomFormatConfigYaml
                        {
                            TrashIds = new[]
                            {
                                "2f22d89048b01681dde8afe203bf2e95"
                            }
                        }
                    },
                    QualityDefinition = new QualitySizeConfigYaml
                    {
                        Type = "series",
                        PreferredRatio = 0.75m
                    },
                    DeleteOldCustomFormats = true
                }
            }
        });
    }
}
