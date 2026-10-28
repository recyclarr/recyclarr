using Recyclarr.Core.TestLibrary;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Core.Tests.ServarrApi.QualityProfile;

internal sealed class RadarrQualityProfileGatewayTest : ServarrHttpTestFixture
{
    [Test]
    public async Task Update_round_trips_fields_unknown_to_recyclarr_at_every_level()
    {
        Respond(
            HttpMethod.Get,
            "/api/v3/qualityprofile",
            """
            [
              {
                "id": 3,
                "name": "HD",
                "upgradeAllowed": true,
                "cutoff": 7,
                "minFormatScore": 0,
                "futureProfileField": "keep",
                "items": [
                  {
                    "quality": {
                      "id": 7,
                      "name": "Bluray-1080p",
                      "source": "futureSource",
                      "modifier": "futureModifier"
                    },
                    "items": [],
                    "allowed": true,
                    "futureItemField": 2
                  }
                ],
                "formatItems": [
                  { "format": 10, "name": "CF", "score": 5, "futureFormatItemField": true }
                ]
              }
            ]
            """
        );
        var sut = ResolveGateway<RadarrQualityProfileGateway>(NewConfig.Radarr());

        var profile = (await sut.GetQualityProfiles(CancellationToken.None)).Single();
        await sut.UpdateQualityProfile(
            profile with
            {
                MinFormatScore = 10,
            },
            CancellationToken.None
        );

        var sent = SentBody(HttpMethod.Put, "/api/v3/qualityprofile/3");
        sent["minFormatScore"]!.GetValue<int>().Should().Be(10);
        sent["futureProfileField"]!.GetValue<string>().Should().Be("keep");
        var item = sent["items"]![0]!;
        item["futureItemField"]!.GetValue<int>().Should().Be(2);
        item["quality"]!["source"]!.GetValue<string>().Should().Be("futureSource");
        item["quality"]!["modifier"]!.GetValue<string>().Should().Be("futureModifier");
        sent["formatItems"]![0]!["futureFormatItemField"]!.GetValue<bool>().Should().BeTrue();
    }
}
