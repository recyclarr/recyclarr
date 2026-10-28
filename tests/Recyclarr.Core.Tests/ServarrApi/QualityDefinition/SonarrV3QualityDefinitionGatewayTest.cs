using Recyclarr.Core.TestLibrary;
using Recyclarr.ServarrApi.QualityDefinition;

namespace Recyclarr.Core.Tests.ServarrApi.QualityDefinition;

internal sealed class SonarrV3QualityDefinitionGatewayTest : ServarrHttpTestFixture
{
    [Test]
    public async Task Update_sends_sizes_and_round_trips_unmanaged_fields()
    {
        Respond(
            HttpMethod.Get,
            "/api/v3/qualitydefinition",
            """
            [
              {
                "id": 4,
                "quality": { "id": 9, "name": "HDTV-1080p", "source": "futureSource" },
                "title": "My HDTV",
                "weight": 12,
                "minSize": 1,
                "maxSize": 100,
                "preferredSize": 50
              }
            ]
            """
        );
        var sut = ResolveGateway<SonarrV3QualityDefinitionGateway>(NewConfig.Sonarr());

        var item = (await sut.GetQualityDefinitions(CancellationToken.None)).Single();
        await sut.UpdateQualityDefinitions(
            [item with { MinSize = 5, MaxSize = 60, PreferredSize = 30 }],
            CancellationToken.None
        );

        var sent = SentBody(HttpMethod.Put, "/api/v3/qualitydefinition/update")[0]!;
        sent["minSize"]!.GetValue<double>().Should().Be(5);
        sent["maxSize"]!.GetValue<double>().Should().Be(60);
        sent["preferredSize"]!.GetValue<double>().Should().Be(30);
        sent["title"]!.GetValue<string>().Should().Be("My HDTV");
        sent["weight"]!.GetValue<int>().Should().Be(12);
        sent["quality"]!["source"]!.GetValue<string>().Should().Be("futureSource");
    }
}
