using System.IO.Abstractions;
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
            .Configs.Should()
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
    public void Deprecated_property_produces_warning_and_continues_sync()
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
                    replace_existing_custom_formats: true
                """
            )
        );

        var result = sut.FindAndLoadConfigs(
            new ConfigFilterCriteria { ManualConfigFiles = ["manual.yml"] }
        );

        result.Configs.Should().ContainSingle();
        result.Failures.Should().BeEmpty();
        result
            .DeprecationWarnings.Should()
            .ContainSingle()
            .Which.Should()
            .Contain("replace_existing_custom_formats");
    }

    [Test]
    public void Deprecated_property_in_include_produces_warning_and_continues_sync()
    {
        var sut = Resolve<ConfigurationRegistry>();

        var includeFile = Paths.YamlIncludeDirectory.File("deprecated-include.yml");
        Fs.AddFile(
            includeFile,
            new MockFileData(
                """
                replace_existing_custom_formats: true
                custom_formats:
                  - trash_ids:
                      - aabbccdd
                """
            )
        );

        Fs.AddFile(
            "manual.yml",
            new MockFileData(
                """
                radarr:
                  instance1:
                    base_url: http://localhost:7878
                    api_key: asdf
                    include:
                      - config: deprecated-include.yml
                """
            )
        );

        var result = sut.FindAndLoadConfigs(
            new ConfigFilterCriteria { ManualConfigFiles = ["manual.yml"] }
        );

        result.Configs.Should().ContainSingle();
        result.Failures.Should().BeEmpty();
        result
            .DeprecationWarnings.Should()
            .ContainSingle()
            .Which.Should()
            .Contain("replace_existing_custom_formats");
    }

    [Test]
    public void Object_in_skip_list_produces_clear_error_instead_of_generic_exception()
    {
        var sut = Resolve<ConfigurationRegistry>();

        Fs.AddFile(
            "config.yml",
            new MockFileData(
                """
                radarr:
                  instance1:
                    base_url: http://localhost:7878
                    api_key: asdf
                    custom_format_groups:
                      skip:
                        - trash_id: 9d5acd8f1da78dfbae788182f7605200
                """
            )
        );

        var result = sut.FindAndLoadConfigs(
            new ConfigFilterCriteria { ManualConfigFiles = ["config.yml"] }
        );

        result.Configs.Should().BeEmpty();
        result
            .Failures.Should()
            .ContainSingle()
            .Which.Message.Should()
            .Contain("not key-value pairs");
    }

    [Test]
    public void Renamed_quality_profiles_in_custom_formats_produces_error()
    {
        var sut = Resolve<ConfigurationRegistry>();

        Fs.AddFile(
            "config.yml",
            new MockFileData(
                """
                radarr:
                  instance1:
                    base_url: http://localhost:7878
                    api_key: asdf
                    custom_formats:
                      - trash_ids:
                          - aabbccdd
                        quality_profiles:
                          - name: TestProfile
                """
            )
        );

        var result = sut.FindAndLoadConfigs(
            new ConfigFilterCriteria { ManualConfigFiles = ["config.yml"] }
        );

        result.Configs.Should().BeEmpty();
        result.Failures.Should().ContainSingle().Which.Message.Should().Contain("assign_scores_to");
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

        var groups = result.Configs.Should().ContainSingle().Which.CustomFormatGroups;

        groups.Skip.Should().BeEquivalentTo("group-to-skip");
        groups
            .Add.Should()
            .BeEquivalentTo([
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
            ]);
    }
}
