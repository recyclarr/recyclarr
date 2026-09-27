using System.Net;
using System.Net.Http.Json;
using Recyclarr.Server.Features.Guide.CustomFormatGroups;
using Recyclarr.Server.Features.Guide.CustomFormats;
using Recyclarr.Server.Features.Guide.ScoreSets;

namespace Recyclarr.Server.Tests.Http;

internal sealed class GuideCustomFormatsHttpTest : GuideHttpFixture
{
    private void AddCustomFormats()
    {
        AddGuideFile(
            "radarr/cf/truehd-atmos.json",
            """
            {
              "trash_id": "cf-atmos",
              "name": "TrueHD ATMOS",
              "trash_scores": { "default": 5000, "SQP-1": 1600 },
              "specifications": []
            }
            """
        );
        AddGuideFile(
            "radarr/cf/uncategorized.json",
            """
            {
              "trash_id": "cf-other",
              "name": "Not In Collection",
              "trash_scores": { "default": 10 },
              "specifications": []
            }
            """
        );
    }

    [Test]
    public async Task Custom_formats_are_listed_with_their_category()
    {
        AddCustomFormats();
        using var client = CreateClient();

        var response = await client.GetFromJsonAsync<ListGuideCustomFormatsResponse>(
            new Uri("/api/v1/guide/radarr/custom-formats", UriKind.Relative)
        );

        response!
            .Items.Should()
            .BeEquivalentTo([
                new GuideCustomFormatSummaryResponse("cf-other", "Not In Collection", null),
                new GuideCustomFormatSummaryResponse("cf-atmos", "TrueHD ATMOS", "Audio Formats"),
            ]);
    }

    [Test]
    public async Task Score_sets_are_the_distinct_score_names_of_custom_formats()
    {
        AddCustomFormats();
        using var client = CreateClient();

        var response = await client.GetFromJsonAsync<ListGuideScoreSetsResponse>(
            new Uri("/api/v1/guide/radarr/score-sets", UriKind.Relative)
        );

        response!
            .Items.Should()
            .Equal(
                new GuideScoreSetSummaryResponse("default"),
                new GuideScoreSetSummaryResponse("SQP-1")
            );
    }

    [Test]
    public async Task Custom_format_groups_are_listed_with_members_and_profiles()
    {
        AddGuideFile(
            "radarr/cf-groups/audio.json",
            """
            {
              "name": "Audio",
              "trash_id": "group-audio",
              "trash_description": "Audio formats",
              "default": "true",
              "custom_formats": [
                { "name": "TrueHD ATMOS", "trash_id": "cf-atmos", "required": true, "default": false },
                { "name": "DTS X", "trash_id": "cf-dtsx", "required": false, "default": true }
              ],
              "quality_profiles": { "include": { "UHD Bluray": "qp-uhd", "HD Bluray": "qp-hd" } }
            }
            """
        );
        using var client = CreateClient();

        var response = await client.GetFromJsonAsync<ListGuideCustomFormatGroupsResponse>(
            new Uri("/api/v1/guide/radarr/custom-format-groups", UriKind.Relative)
        );

        response!
            .Items.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new GuideCustomFormatGroupSummaryResponse(
                    "group-audio",
                    "Audio",
                    Default: true,
                    [
                        new GuideCustomFormatGroupMemberResponse(
                            "cf-dtsx",
                            "DTS X",
                            Required: false,
                            Default: true
                        ),
                        new GuideCustomFormatGroupMemberResponse(
                            "cf-atmos",
                            "TrueHD ATMOS",
                            Required: true,
                            Default: false
                        ),
                    ],
                    ["HD Bluray", "UHD Bluray"]
                ),
                o => o.WithStrictOrdering()
            );
    }

    [Test]
    public async Task Service_namespace_does_not_resolve()
    {
        using var client = CreateClient();

        var response = await client.GetAsync(new Uri("/api/v1/guide/radarr", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestCase("lidarr")]
    [TestCase("999")]
    public async Task Invalid_service_returns_bad_request(string service)
    {
        using var client = CreateClient();

        var response = await client.GetAsync(
            new Uri($"/api/v1/guide/{service}/custom-formats", UriKind.Relative)
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }
}
