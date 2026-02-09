using Recyclarr.Config;
using Recyclarr.Config.Filtering;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Core.TestLibrary;

namespace Recyclarr.Core.Tests.IntegrationTests;

[CoreDataSource]
internal sealed class ConfigurationRegistryTest(ConfigurationRegistry sut, MockFileSystem fs)
{
    [Test]
    public void Use_explicit_paths_instead_of_default()
    {
        fs.AddFile(
            "manual.yml",
            new MockFileData(
                """
                radarr:
                  instance1:
                    base_url: http://localhost:7878
                    api_key: asdf
                """
            )
        );

        var result = sut.FindAndLoadConfigs(
            new ConfigFilterCriteria { ManualConfigFiles = ["manual.yml"] }
        );

        result
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new
                {
                    BaseUrl = new Uri("http://localhost:7878"),
                    ApiKey = "asdf",
                    InstanceName = "instance1",
                }
            );
    }

    [Test]
    public void Throw_on_invalid_config_files()
    {
        var act = () =>
            sut.FindAndLoadConfigs(new ConfigFilterCriteria { ManualConfigFiles = ["manual.yml"] });

        act.Should().ThrowExactly<InvalidConfigurationFilesException>();
    }

    [Test]
    public void Parse_custom_format_groups()
    {
        fs.AddFile(
            "config.yml",
            new MockFileData(
                """
                radarr:
                  instance1:
                    base_url: http://localhost:7878
                    api_key: test-key
                    custom_format_groups:
                      skip:
                        - group-to-skip
                      add:
                        - trash_id: anime-web-tier-01
                          assign_scores_to:
                            - trash_id: profile-trash-id-1
                            - trash_id: profile-trash-id-2
                          select:
                            - cf-to-select-1
                            - cf-to-select-2
                """
            )
        );

        var result = sut.FindAndLoadConfigs(
            new ConfigFilterCriteria { ManualConfigFiles = ["config.yml"] }
        );

        var groups = result.Should().ContainSingle().Which.CustomFormatGroups;

        groups.Skip.Should().BeEquivalentTo("group-to-skip");
        groups
            .Add.Should()
            .BeEquivalentTo(
                new[]
                {
                    new
                    {
                        TrashId = "anime-web-tier-01",
                        AssignScoresTo = new[]
                        {
                            new { TrashId = "profile-trash-id-1" },
                            new { TrashId = "profile-trash-id-2" },
                        },
                        Select = new[] { "cf-to-select-1", "cf-to-select-2" },
                    },
                }
            );
    }
}
