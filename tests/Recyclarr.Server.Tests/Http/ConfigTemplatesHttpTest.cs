using System.IO.Abstractions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Recyclarr.Config.Parsing;
using Recyclarr.Server.Features.ConfigIncludes.List;
using Recyclarr.Server.Features.ConfigTemplates.List;
using Recyclarr.TrashGuide;

namespace Recyclarr.Server.Tests.Http;

internal sealed class ConfigTemplatesHttpTest : GuideHttpFixture
{
    // The server writes enums as camelCase strings.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private void AddProfile(string service, string fileStem, string trashId, string name)
    {
        AddGuideFile(
            $"{service}/quality-profiles/{fileStem}.json",
            $$"""{ "trash_id": "{{trashId}}", "name": "{{name}}", "items": [], "formatItems": {} }"""
        );
    }

    [Test]
    public async Task Template_ids_are_profile_file_names_prefixed_only_on_cross_service_collision()
    {
        AddProfile("radarr", "hd-bluray-web", "qp-hd", "HD Bluray + WEB");
        AddProfile("radarr", "anime-remux-1080p", "qp-ra", "Remux-1080p - Anime");
        AddProfile("sonarr", "anime-remux-1080p", "qp-sa", "Remux-1080p - Anime");
        AddProfile("radarr", "base-profile", "qp-base", "Base Profile");
        AddGuideFile(
            "radarr/quality-profile-groups/groups.json",
            """[{ "name": "TEST", "profiles": { "base-profile": "qp-base" } }]"""
        );
        using var client = CreateClient();

        var response = await client.GetFromJsonAsync<ListConfigTemplatesResponse>(
            new Uri("/api/v1/config-templates", UriKind.Relative),
            JsonOptions
        );

        response!
            .Items.Select(t => (t.Id, t.Service, t.ProfileTrashId))
            .Should()
            .Equal(
                ("hd-bluray-web", SupportedServices.Radarr, "qp-hd"),
                ("radarr-anime-remux-1080p", SupportedServices.Radarr, "qp-ra"),
                ("sonarr-anime-remux-1080p", SupportedServices.Sonarr, "qp-sa")
            );
    }

    [Test]
    public async Task Template_is_yaml_that_loads_as_a_config_for_the_profile()
    {
        AddProfile("radarr", "hd-bluray-web", "qp-hd", "HD Bluray + WEB");
        AddGuideFile(
            "radarr/cf-groups/golden-rule.json",
            """
            {
              "trash_id": "grp-golden", "name": "[Optional] Golden Rule HD", "default": "false",
              "custom_formats": [
                { "trash_id": "cf-x265-hd", "name": "x265 (HD)", "default": true },
                { "trash_id": "cf-x265-nohdr", "name": "x265 (no HDR/DV)" }
              ],
              "quality_profiles": { "include": { "HD Bluray + WEB": "qp-hd" } }
            }
            """
        );
        AddGuideFile(
            "radarr/conflicts.json",
            """
            { "custom_formats": [
              { "cf-x265-hd": { "name": "x265 (HD)" }, "cf-x265-nohdr": { "name": "x" } }
            ] }
            """
        );
        using var client = CreateClient();

        var response = await client.GetAsync(
            new Uri("/api/v1/config-templates/HD-BLURAY-WEB", UriKind.Relative)
        );

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/yaml");
        var yaml = await response.Content.ReadAsStringAsync();
        yaml.Should()
            .Contain(
                """
                        # - trash_id: grp-golden  # [Optional] Golden Rule HD
                        #   # Mutually exclusive: enable only one of the following.
                        #   exclude:
                        #     # - cf-x265-hd  # x265 (HD)
                        #   select:
                        #     # - cf-x265-nohdr  # x265 (no HDR/DV)
                """.ReplaceLineEndings("\n")
            );

        var loaded = Services.GetRequiredService<ConfigurationLoader>().Load(yaml);
        loaded
            .Should()
            .ContainSingle()
            .Which.Should()
            .Match<LoadedConfigYaml>(c =>
                c.InstanceName == "hd-bluray-web"
                && c.ServiceType == SupportedServices.Radarr
                && c.Yaml.QualityDefinition!.Type == "movie"
                && c.Yaml.QualityProfiles!.Single().TrashId == "qp-hd"
            );
    }

    [Test]
    public async Task Includes_from_config_templates_providers_are_listed()
    {
        var includes = Fs.CurrentDirectory().SubDirectory("includes");
        Fs.AddFile(
            includes.File("includes.json"),
            new MockFileData(
                """
                {
                  "radarr": [{ "id": "radarr-inc", "template": "r.yml" }],
                  "sonarr": [{ "id": "sonarr-inc", "template": "s.yml" }]
                }
                """
            )
        );
        Fs.AddFile(
            Paths.ConfigDirectory.File("settings.yml"),
            new MockFileData(
                $"""
                resource_providers:
                  - name: my-includes
                    type: config-templates
                    path: {includes.FullName}
                """
            )
        );
        using var client = CreateClient();

        var response = await client.GetFromJsonAsync<ListConfigIncludesResponse>(
            new Uri("/api/v1/config-includes", UriKind.Relative),
            JsonOptions
        );

        response!
            .Items.Select(i => (i.Id, i.Service))
            .Should()
            .Equal(
                ("radarr-inc", SupportedServices.Radarr),
                ("sonarr-inc", SupportedServices.Sonarr)
            );
    }

    [Test]
    public async Task Unknown_template_id_is_not_found()
    {
        using var client = CreateClient();

        var response = await client.GetAsync(
            new Uri("/api/v1/config-templates/missing", UriKind.Relative)
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
