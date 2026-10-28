using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;
using Recyclarr.ServarrApi.MediaManagement;

namespace Recyclarr.Core.Tests.ServarrApi.MediaManagement;

internal sealed class SonarrV5MediaManagementGatewayTest : ServarrHttpTestFixture
{
    private const string Path = "/api/v5/settings/mediamanagement";

    [Test]
    public async Task Get_reads_unknown_propers_mode_as_unset()
    {
        Respond(HttpMethod.Get, Path, """{ "id": 1, "downloadPropersAndRepacks": "futureMode" }""");
        var sut = ResolveGateway<SonarrV5MediaManagementGateway>(NewConfig.Sonarr());

        var result = await sut.GetMediaManagement(CancellationToken.None);

        result.PropersAndRepacks.Should().BeNull();
    }

    [Test]
    public async Task Update_changes_owned_field_and_round_trips_the_rest()
    {
        Respond(
            HttpMethod.Get,
            Path,
            """
            {
              "id": 1,
              "downloadPropersAndRepacks": "preferAndUpgrade",
              "recycleBin": "/recycle",
              "seasonPackUpgrade": "futureUpgradeMode",
              "seasonPackUpgradeThreshold": 75,
              "futureSetting": { "enabled": true }
            }
            """
        );
        var sut = ResolveGateway<SonarrV5MediaManagementGateway>(NewConfig.Sonarr());

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
                """{"id":1,"downloadPropersAndRepacks":"doNotPrefer","recycleBin":"/recycle","seasonPackUpgrade":"futureUpgradeMode","seasonPackUpgradeThreshold":75,"futureSetting":{"enabled":true}}"""
            );
    }
}
