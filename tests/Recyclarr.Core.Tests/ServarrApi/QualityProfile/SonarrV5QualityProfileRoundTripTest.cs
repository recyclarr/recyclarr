using Recyclarr.Core.TestLibrary;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Core.Tests.ServarrApi.QualityProfile;

internal sealed class SonarrV5QualityProfileRoundTripTest : ServarrHttpTestFixture
{
    [Test]
    public async Task Update_round_trips_fields_unknown_to_recyclarr_at_every_level()
    {
        Respond(
            HttpMethod.Get,
            "/api/v5/qualityprofile",
            """
            [
              {
                "id": 3,
                "name": "WEB-1080p",
                "cutoff": 9,
                "futureProfileField": "keep",
                "items": [
                  {
                    "quality": { "id": 9, "name": "HDTV-1080p", "source": "futureSource" },
                    "items": [],
                    "allowed": true,
                    "minSize": 5,
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
        var sut = ResolveGateway<SonarrV5QualityProfileGateway>(NewConfig.Sonarr());

        var profile = (await sut.GetQualityProfiles(CancellationToken.None)).Single();
        await sut.UpdateQualityProfile(
            profile with
            {
                MinFormatScore = 10,
            },
            CancellationToken.None
        );

        var sent = SentBody(HttpMethod.Put, "/api/v5/qualityprofile/3");
        sent["minFormatScore"]!.GetValue<int>().Should().Be(10);
        sent["futureProfileField"]!.GetValue<string>().Should().Be("keep");
        var item = sent["items"]![0]!;
        item["futureItemField"]!.GetValue<int>().Should().Be(2);
        item["minSize"]!.GetValue<double>().Should().Be(5);
        item["quality"]!["source"]!.GetValue<string>().Should().Be("futureSource");
        sent["formatItems"]![0]!["futureFormatItemField"]!.GetValue<bool>().Should().BeTrue();
    }
}
