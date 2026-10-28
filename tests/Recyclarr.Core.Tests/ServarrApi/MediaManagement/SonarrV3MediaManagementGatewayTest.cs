using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;
using Recyclarr.ServarrApi.MediaManagement;

namespace Recyclarr.Core.Tests.ServarrApi.MediaManagement;

internal sealed class SonarrV3MediaManagementGatewayTest : ServarrHttpTestFixture
{
    private const string Path = "/api/v3/config/mediamanagement";

    [Test]
    public async Task Get_reads_unknown_propers_mode_as_unset()
    {
        Respond(HttpMethod.Get, Path, """{ "id": 1, "downloadPropersAndRepacks": "futureMode" }""");
        var sut = ResolveGateway<SonarrV3MediaManagementGateway>(NewConfig.Sonarr());

        var result = await sut.GetMediaManagement(CancellationToken.None);

        result.PropersAndRepacks.Should().BeNull();
    }

    // Fields Sonarr adds after Recyclarr's contract was written must survive the update
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
              "seasonPackUpgrade": "threshold",
              "seasonPackUpgradeThreshold": 75,
              "skipFreeSpaceCheckWhenGrabbing": true,
              "futureSetting": { "enabled": true, "limit": 5 }
            }
            """
        );
        var sut = ResolveGateway<SonarrV3MediaManagementGateway>(NewConfig.Sonarr());

        var fetched = await sut.GetMediaManagement(CancellationToken.None);
        fetched.PropersAndRepacks.Should().Be(PropersAndRepacksMode.PreferAndUpgrade);
        await sut.UpdateMediaManagement(
            fetched with
            {
                PropersAndRepacks = PropersAndRepacksMode.DoNotPrefer,
            },
            CancellationToken.None
        );

        SentBody(HttpMethod.Put, $"{Path}/1")
            .ToJsonString()
            .Should()
            .Be(
                """{"id":1,"downloadPropersAndRepacks":"doNotPrefer","seasonPackUpgrade":"threshold","seasonPackUpgradeThreshold":75,"skipFreeSpaceCheckWhenGrabbing":true,"futureSetting":{"enabled":true,"limit":5}}"""
            );
    }
}
