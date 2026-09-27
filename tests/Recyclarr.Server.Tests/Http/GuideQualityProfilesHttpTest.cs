using System.Net.Http.Json;
using Recyclarr.Server.Features.Guide.Qualities;
using Recyclarr.Server.Features.Guide.QualityProfiles;

namespace Recyclarr.Server.Tests.Http;

internal sealed class GuideQualityProfilesHttpTest : GuideHttpFixture
{
    [Test]
    public async Task Quality_profiles_list_allowed_qualities_and_scored_custom_formats()
    {
        AddGuideFile(
            "sonarr/quality-profiles/web-1080p.json",
            """
            {
              "trash_id": "qp-web",
              "name": "WEB-1080p",
              "trash_url": "https://trash-guides.info/web-1080p",
              "trash_score_set": "sqp-1",
              "items": [
                { "name": "WEB 1080p", "allowed": true, "items": ["WEBDL-1080p", "WEBRip-1080p"] },
                { "name": "Bluray-1080p", "allowed": true, "items": [] },
                { "name": "HDTV-720p", "allowed": false, "items": [] }
              ],
              "formatItems": { "x265 (HD)": "cf-x265", "BR-DISK": "cf-brdisk" }
            }
            """
        );
        AddGuideFile(
            "sonarr/quality-profiles/anime.json",
            """
            { "trash_id": "qp-anime", "name": "Anime", "items": [], "formatItems": {} }
            """
        );
        using var client = CreateClient();

        var response = await client.GetFromJsonAsync<ListGuideQualityProfilesResponse>(
            new Uri("/api/v1/guide/sonarr/quality-profiles", UriKind.Relative)
        );

        response!
            .Items.Should()
            .BeEquivalentTo(
                [
                    new GuideQualityProfileSummaryResponse("qp-anime", "Anime", null, null, [], []),
                    new GuideQualityProfileSummaryResponse(
                        "qp-web",
                        "WEB-1080p",
                        "https://trash-guides.info/web-1080p",
                        "sqp-1",
                        [
                            new GuideProfileQualityResponse(
                                "WEB 1080p",
                                ["WEBDL-1080p", "WEBRip-1080p"]
                            ),
                            new GuideProfileQualityResponse("Bluray-1080p", []),
                        ],
                        ["BR-DISK", "x265 (HD)"]
                    ),
                ],
                o => o.WithStrictOrdering()
            );
    }

    [Test]
    public async Task Qualities_list_each_quality_size_type()
    {
        AddGuideFile("radarr/quality-size/movie.json", """{ "type": "movie", "qualities": [] }""");
        AddGuideFile(
            "radarr/quality-size/sqp-streaming.json",
            """{ "type": "sqp-streaming", "qualities": [] }"""
        );
        using var client = CreateClient();

        var response = await client.GetFromJsonAsync<ListGuideQualitiesResponse>(
            new Uri("/api/v1/guide/radarr/qualities", UriKind.Relative)
        );

        response!
            .Items.Should()
            .Equal(
                new GuideQualitySummaryResponse("movie"),
                new GuideQualitySummaryResponse("sqp-streaming")
            );
    }
}
