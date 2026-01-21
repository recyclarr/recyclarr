using Autofac;
using Recyclarr.Config;
using Recyclarr.Config.Filtering;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Core.TestLibrary;
using Recyclarr.TestLibrary.Autofac;

namespace Recyclarr.Core.Tests.IntegrationTests;

internal sealed class ConfigurationRegistryTest : IntegrationTestFixture
{
    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        base.RegisterStubsAndMocks(builder);

        // ConfigurationRegistry uses ConfigFilterProcessor which depends on
        // IFilterResultRenderer, so we need to register a mock for it since we
        // don't care about rendering logic for this test.
        builder.RegisterMockFor<IFilterResultRenderer>();
    }

    [Test]
    public void Use_explicit_paths_instead_of_default()
    {
        var sut = Resolve<ConfigurationRegistry>();

        Fs.AddFile(
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
        var sut = Resolve<ConfigurationRegistry>();

        var act = () =>
            sut.FindAndLoadConfigs(new ConfigFilterCriteria { ManualConfigFiles = ["manual.yml"] });

        act.Should().ThrowExactly<InvalidConfigurationFilesException>();
    }

    [Test]
    public void Parse_custom_format_groups()
    {
        var sut = Resolve<ConfigurationRegistry>();

        Fs.AddFile(
            "config.yml",
            new MockFileData(
                """
                radarr:
                  instance1:
                    base_url: http://localhost:7878
                    api_key: test-key
                    custom_format_groups:
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

        result
            .Should()
            .ContainSingle()
            .Which.CustomFormatGroups.Should()
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
