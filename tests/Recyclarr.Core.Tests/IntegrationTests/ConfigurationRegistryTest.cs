using System.IO.Abstractions;
using Autofac;
using Recyclarr.Config;
using Recyclarr.Config.Filtering;
using Recyclarr.Config.Models;
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
    public void Trailing_slash_stripped_from_base_url()
    {
        var sut = Resolve<ConfigurationRegistry>();

        Fs.AddFile(
            "manual.yml",
            new MockFileData(
                """
                radarr:
                  instance1:
                    base_url: http://localhost:7878/radarr/
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
            .Which.BaseUrl.Should()
            .Be(new Uri("http://localhost:7878/radarr"));
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
    public void Empty_custom_formats_is_no_op()
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
                """
            )
        );

        var result = sut.FindAndLoadConfigs(
            new ConfigFilterCriteria { ManualConfigFiles = ["config.yml"] }
        );

        result.Failures.Should().BeEmpty();
        result.Configs.Should().ContainSingle().Which.CustomFormats.Should().BeEmpty();
    }

    [Test]
    public void Empty_quality_profiles_is_no_op()
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
                    quality_profiles:
                """
            )
        );

        var result = sut.FindAndLoadConfigs(
            new ConfigFilterCriteria { ManualConfigFiles = ["config.yml"] }
        );

        result.Failures.Should().BeEmpty();
        result.Configs.Should().ContainSingle().Which.QualityProfiles.Should().BeEmpty();
    }

    [Test]
    public void Empty_custom_format_groups_add_is_no_op()
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
                      add:
                """
            )
        );

        var result = sut.FindAndLoadConfigs(
            new ConfigFilterCriteria { ManualConfigFiles = ["config.yml"] }
        );

        result.Failures.Should().BeEmpty();
        result.Configs.Should().ContainSingle().Which.CustomFormatGroups.Add.Should().BeEmpty();
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

    [Test]
    public void Entry_level_score_default_resolves_in_runtime_config()
    {
        // End-to-end: YAML parse -> include merge -> runtime mapping. The entry-level `score`
        // under `custom_formats` must land on every per-profile entry that did not define its
        // own score, and per-profile scores must still override.
        var sut = Resolve<ConfigurationRegistry>();

        var templatePath = Paths.YamlIncludeDirectory.File("template.yml");
        Fs.AddFile(
            templatePath,
            new MockFileData(
                """
                custom_formats:
                  - trash_ids:
                      - dc98083864ea246d05a42df0d05f81cc
                    score: 0
                    assign_scores_to:
                      - name: MULTi-VF-HD
                      - name: MULTi-VO-HD
                      - name: REMUX-MULTi-VF-HD
                        score: 100
                """
            )
        );

        Fs.AddFile(
            "config.yml",
            new MockFileData(
                """
                radarr:
                  instance1:
                    base_url: http://localhost:7878
                    api_key: test-key
                    include:
                      - config: template.yml
                """
            )
        );

        var result = sut.FindAndLoadConfigs(
            new ConfigFilterCriteria { ManualConfigFiles = ["config.yml"] }
        );

        result.Failures.Should().BeEmpty();

        var cf = result
            .Configs.Should()
            .ContainSingle()
            .Which.CustomFormats.Should()
            .ContainSingle()
            .Subject;

        cf.TrashIds.Should().BeEquivalentTo("dc98083864ea246d05a42df0d05f81cc");
        cf.AssignScoresTo.Should()
            .BeEquivalentTo([
                new AssignScoresToConfig { Name = "MULTi-VF-HD", Score = 0 },
                new AssignScoresToConfig { Name = "MULTi-VO-HD", Score = 0 },
                new AssignScoresToConfig { Name = "REMUX-MULTi-VF-HD", Score = 100 },
            ]);
    }
}
