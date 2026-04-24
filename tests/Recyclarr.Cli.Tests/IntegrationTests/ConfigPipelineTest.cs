using System.IO.Abstractions;
using Autofac;
using Recyclarr.Cli.Processors;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Config.Filtering;
using Recyclarr.Config.Models;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.TestLibrary.Autofac;

namespace Recyclarr.Cli.Tests.IntegrationTests;

internal sealed class ConfigPipelineTest : CliIntegrationFixture
{
    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        base.RegisterStubsAndMocks(builder);

        // ConfigFilterProcessor depends on IFilterResultRenderer; use a mock since
        // rendering behavior is not under test here.
        builder.RegisterMockFor<IFilterResultRenderer>();
    }

    [Test]
    public void Trailing_slash_stripped_from_base_url()
    {
        var factory = Resolve<ConfigPipelineFactory>();

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

        var configs = factory.FromPaths(["manual.yml"]).GetConfigs();

        configs
            .Should()
            .ContainSingle()
            .Which.BaseUrl.Should()
            .Be(new Uri("http://localhost:7878/radarr"));
    }

    [Test]
    public void Use_explicit_paths_instead_of_default()
    {
        var factory = Resolve<ConfigPipelineFactory>();

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

        var configs = factory.FromPaths(["manual.yml"]).GetConfigs();

        configs
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
        var factory = Resolve<ConfigPipelineFactory>();

        var act = () => factory.FromPaths(["manual.yml"]);

        act.Should().ThrowExactly<InvalidConfigurationFilesException>();
    }

    [Test]
    public void Deprecated_property_produces_warning_and_continues_sync()
    {
        var factory = Resolve<ConfigPipelineFactory>();
        var diagnostics = Resolve<IConfigDiagnosticCollector>();

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

        var configs = factory.FromPaths(["manual.yml"]).GetConfigs();

        configs.Should().ContainSingle();
        diagnostics
            .Deprecations.Should()
            .ContainSingle()
            .Which.Should()
            .Contain("replace_existing_custom_formats");
    }

    [Test]
    public void Deprecated_property_in_include_produces_warning_and_continues_sync()
    {
        var factory = Resolve<ConfigPipelineFactory>();
        var diagnostics = Resolve<IConfigDiagnosticCollector>();

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

        var configs = factory.FromPaths(["manual.yml"]).GetConfigs();

        configs.Should().ContainSingle();
        diagnostics
            .Deprecations.Should()
            .ContainSingle()
            .Which.Should()
            .Contain("replace_existing_custom_formats");
    }

    [Test]
    public void Object_in_skip_list_produces_clear_error_instead_of_generic_exception()
    {
        var factory = Resolve<ConfigPipelineFactory>();

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

        var configs = factory.FromPaths(["config.yml"]).GetConfigs();

        configs.Should().BeEmpty();
    }

    [Test]
    public void Renamed_quality_profiles_in_custom_formats_produces_error()
    {
        var factory = Resolve<ConfigPipelineFactory>();

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

        var configs = factory.FromPaths(["config.yml"]).GetConfigs();

        configs.Should().BeEmpty();
    }

    [Test]
    public void Empty_custom_formats_is_no_op()
    {
        var factory = Resolve<ConfigPipelineFactory>();

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

        var configs = factory.FromPaths(["config.yml"]).GetConfigs();

        configs.Should().ContainSingle().Which.CustomFormats.Should().BeEmpty();
    }

    [Test]
    public void Empty_quality_profiles_is_no_op()
    {
        var factory = Resolve<ConfigPipelineFactory>();

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

        var configs = factory.FromPaths(["config.yml"]).GetConfigs();

        configs.Should().ContainSingle().Which.QualityProfiles.Should().BeEmpty();
    }

    [Test]
    public void Empty_custom_format_groups_add_is_no_op()
    {
        var factory = Resolve<ConfigPipelineFactory>();

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

        var configs = factory.FromPaths(["config.yml"]).GetConfigs();

        configs.Should().ContainSingle().Which.CustomFormatGroups.Add.Should().BeEmpty();
    }

    [Test]
    public void Parse_custom_format_groups()
    {
        var factory = Resolve<ConfigPipelineFactory>();

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

        var configs = factory.FromPaths(["config.yml"]).GetConfigs();

        var groups = configs.Should().ContainSingle().Which.CustomFormatGroups;

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
        var factory = Resolve<ConfigPipelineFactory>();

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

        var configs = factory.FromPaths(["config.yml"]).GetConfigs();

        var cf = configs
            .Should()
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
