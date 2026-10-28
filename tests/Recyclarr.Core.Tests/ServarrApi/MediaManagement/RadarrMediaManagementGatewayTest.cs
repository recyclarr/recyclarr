using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Servarr.MediaManagement;
using Recyclarr.ServarrApi.MediaManagement;

namespace Recyclarr.Core.Tests.ServarrApi.MediaManagement;

internal sealed class RadarrMediaManagementGatewayTest : ServarrHttpTestFixture
{
    private const string Path = "/api/v3/config/mediamanagement";

    [Test]
    public async Task Get_maps_propers_mode()
    {
        Respond(
            HttpMethod.Get,
            Path,
            """{ "id": 5, "downloadPropersAndRepacks": "doNotUpgrade" }"""
        );
        var sut = ResolveGateway<RadarrMediaManagementGateway>(NewConfig.Radarr());

        var result = await sut.GetMediaManagement(CancellationToken.None);

        result
            .Should()
            .BeEquivalentTo(
                new MediaManagementData
                {
                    Id = 5,
                    PropersAndRepacks = PropersAndRepacksMode.DoNotUpgrade,
                }
            );
    }

    [Test]
    public async Task Get_reads_unknown_propers_mode_as_unset()
    {
        Respond(HttpMethod.Get, Path, """{ "id": 1, "downloadPropersAndRepacks": "futureMode" }""");
        var sut = ResolveGateway<RadarrMediaManagementGateway>(NewConfig.Radarr());

        var result = await sut.GetMediaManagement(CancellationToken.None);

        result.PropersAndRepacks.Should().BeNull();
    }

    [Test]
    public async Task Update_sends_mode_and_round_trips_unmanaged_fields()
    {
        Respond(
            HttpMethod.Get,
            Path,
            """
            {
              "id": 1,
              "downloadPropersAndRepacks": "preferAndUpgrade",
              "recycleBin": "/recycle",
              "fileDate": "futureFileDate",
              "futureSetting": { "enabled": true, "limit": 5 }
            }
            """
        );
        var sut = ResolveGateway<RadarrMediaManagementGateway>(NewConfig.Radarr());

        var fetched = await sut.GetMediaManagement(CancellationToken.None);
        await sut.UpdateMediaManagement(
            fetched with
            {
                PropersAndRepacks = PropersAndRepacksMode.DoNotPrefer,
            },
            CancellationToken.None
        );

        var sent = SentBody(HttpMethod.Put, $"{Path}/1");
        sent.ToJsonString()
            .Should()
            .Be(
                """{"id":1,"downloadPropersAndRepacks":"doNotPrefer","recycleBin":"/recycle","fileDate":"futureFileDate","futureSetting":{"enabled":true,"limit":5}}"""
            );
    }

    [Test]
    public async Task Update_without_mode_keeps_unknown_server_value()
    {
        Respond(HttpMethod.Get, Path, """{ "id": 1, "downloadPropersAndRepacks": "futureMode" }""");
        var sut = ResolveGateway<RadarrMediaManagementGateway>(NewConfig.Radarr());

        var fetched = await sut.GetMediaManagement(CancellationToken.None);
        await sut.UpdateMediaManagement(fetched, CancellationToken.None);

        SentBody(HttpMethod.Put, $"{Path}/1")["downloadPropersAndRepacks"]!
            .GetValue<string>()
            .Should()
            .Be("futureMode");
    }
}
