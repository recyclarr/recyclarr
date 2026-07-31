using System.IO.Abstractions;
using Recyclarr.Config.Models;
using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.ErrorHandling;

namespace Recyclarr.Core.Tests.IntegrationTests;

internal sealed class ConfigurationBehaviorIntegrationTest : CoreIntegrationTestFixture
{
    [Test]
    public void Trailing_slash_stripped_from_base_url()
    {
        var config = LoadRadarrConfig(
            """
            radarr:
              instance1:
                base_url: http://localhost:7878/radarr/
                api_key: asdf
            """
        );

        config.BaseUrl.Should().Be(new Uri("http://localhost:7878/radarr"));
    }

    [Test]
    public void Explicit_path_is_retained_in_runtime_config()
    {
        var config = LoadRadarrConfig(
            """
            radarr:
              instance1:
                base_url: http://localhost:7878
                api_key: asdf
            """,
            "manual.yml"
        );

        config.BaseUrl.Should().Be(new Uri("http://localhost:7878"));
        config.ApiKey.Should().Be("asdf");
        config.InstanceName.Should().Be("instance1");
        config.YamlPath.Should().NotBeNull();
        config.YamlPath?.FullName.Should().Be("/manual.yml");
    }

    [Test]
    public void Deprecated_property_produces_warning_and_continues_loading()
    {
        var config = LoadRadarrConfig(
            """
            radarr:
              instance1:
                base_url: http://localhost:7878
                api_key: asdf
                replace_existing_custom_formats: true
            """
        );

        config.Should().NotBeNull();
        Resolve<IConfigDiagnosticCollector>()
            .Deprecations.Should()
            .ContainSingle()
            .Which.Should()
            .Contain("replace_existing_custom_formats");
    }

    [Test]
    public void Deprecated_property_in_include_produces_warning_and_continues_loading()
    {
        Fs.AddFile(
            Paths.YamlIncludeDirectory.File("deprecated-include.yml"),
            new MockFileData(
                """
                replace_existing_custom_formats: true
                custom_formats:
                  - trash_ids:
                      - aabbccdd
                """
            )
        );

        var config = LoadRadarrConfig(
            """
            radarr:
              instance1:
                base_url: http://localhost:7878
                api_key: asdf
                include:
                  - config: deprecated-include.yml
            """
        );

        config.Should().NotBeNull();
        Resolve<IConfigDiagnosticCollector>()
            .Deprecations.Should()
            .ContainSingle()
            .Which.Should()
            .Contain("replace_existing_custom_formats");
    }

    [Test]
    public void Object_in_skip_list_produces_config_parsing_exception()
    {
        var act = () =>
            LoadFile(
                """
                radarr:
                  instance1:
                    base_url: http://localhost:7878
                    api_key: asdf
                    custom_format_groups:
                      skip:
                        - trash_id: 9d5acd8f1da78dfbae788182f7605200
                """
            );

        act.Should().Throw<ConfigParsingException>();
    }

    [Test]
    public void Renamed_quality_profiles_in_custom_formats_produces_config_parsing_exception()
    {
        var act = () =>
            LoadFile(
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
            );

        act.Should().Throw<ConfigParsingException>();
    }

    [Test]
    public void Empty_custom_formats_is_no_op()
    {
        var config = LoadRadarrConfig(
            """
            radarr:
              instance1:
                base_url: http://localhost:7878
                api_key: asdf
                custom_formats:
            """
        );

        config.CustomFormats.Should().BeEmpty();
    }

    [Test]
    public void Empty_quality_profiles_is_no_op()
    {
        var config = LoadRadarrConfig(
            """
            radarr:
              instance1:
                base_url: http://localhost:7878
                api_key: asdf
                quality_profiles:
            """
        );

        config.QualityProfiles.Should().BeEmpty();
    }

    [Test]
    public void Empty_custom_format_groups_add_is_no_op()
    {
        var config = LoadRadarrConfig(
            """
            radarr:
              instance1:
                base_url: http://localhost:7878
                api_key: asdf
                custom_format_groups:
                  add:
            """
        );

        config.CustomFormatGroups.Add.Should().BeEmpty();
    }

    [Test]
    public void Parse_custom_format_groups()
    {
        var config = LoadRadarrConfig(
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
        );

        config.CustomFormatGroups.Skip.Should().BeEquivalentTo("group-to-skip");
        config
            .CustomFormatGroups.Add.Should()
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

    private RadarrConfiguration LoadRadarrConfig(string yaml, string fileName = "config.yml")
    {
        var loaded = LoadFile(yaml, fileName).Should().ContainSingle().Which;
        var config = loaded.Yaml.Should().BeOfType<RadarrConfigYaml>().Which;
        return (RadarrConfiguration)
            config.ToRadarrConfiguration(loaded.InstanceName, loaded.YamlPath);
    }

    private IReadOnlyCollection<LoadedConfigYaml> LoadFile(
        string yaml,
        string fileName = "config.yml"
    )
    {
        IFileInfo file = Fs.CurrentDirectory().File(fileName);
        Fs.AddFile(file, new MockFileData(yaml));
        return Resolve<ConfigurationLoader>().Load(file);
    }
}
