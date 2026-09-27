using System.Text.Json;

namespace Recyclarr.Server.Tests.Http;

// Asserts the wire JSON because the response is polymorphic by service.
internal sealed class GuideNamingHttpTest : GuideHttpFixture
{
    [Test]
    public async Task Sonarr_naming_lists_formats_with_optional_versions()
    {
        AddGuideFile(
            "sonarr/naming/sonarr-naming.json",
            """
            {
              "season": { "default": "Season {season:00}" },
              "series": { "default": "{Series TitleYear}" },
              "episodes": {
                "standard": { "default:4": "{Series TitleYear} - S{season:00}E{episode:00}" },
                "daily": {},
                "anime": {}
              }
            }
            """
        );
        using var client = CreateClient();

        using var json = JsonDocument.Parse(
            await client.GetStringAsync(new Uri("/api/v1/guide/sonarr/naming", UriKind.Relative))
        );
        var root = json.RootElement;

        root.GetProperty("service").GetString().Should().Be("sonarr");
        var standard = root.GetProperty("standardEpisode")[0];
        standard.GetProperty("key").GetString().Should().Be("default");
        standard.GetProperty("version").GetString().Should().Be("4");
        standard
            .GetProperty("format")
            .GetString()
            .Should()
            .Be("{Series TitleYear} - S{season:00}E{episode:00}");
        var season = root.GetProperty("seasonFolder")[0];
        season.GetProperty("key").GetString().Should().Be("default");
        season.TryGetProperty("version", out _).Should().BeFalse();
    }

    [Test]
    public async Task Radarr_naming_lists_folder_and_file_formats()
    {
        AddGuideFile(
            "radarr/naming/radarr-naming.json",
            """
            {
              "folder": { "default": "{Movie CleanTitle} ({Release Year})" },
              "file": { "standard": "{Movie CleanTitle} {edition-{Edition Tags}}" }
            }
            """
        );
        using var client = CreateClient();

        using var json = JsonDocument.Parse(
            await client.GetStringAsync(new Uri("/api/v1/guide/radarr/naming", UriKind.Relative))
        );
        var root = json.RootElement;

        root.GetProperty("service").GetString().Should().Be("radarr");
        root.GetProperty("movieFolder")[0].GetProperty("key").GetString().Should().Be("default");
        root.GetProperty("standardMovie")[0]
            .GetProperty("format")
            .GetString()
            .Should()
            .Be("{Movie CleanTitle} {edition-{Edition Tags}}");
    }
}
