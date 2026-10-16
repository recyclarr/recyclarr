using System.Text.Json;
using Recyclarr.Config.Models;
using Recyclarr.Json;
using Recyclarr.ServarrApi.MediaManagement;
using SonarrApi = Recyclarr.Api.Sonarr.V5;

namespace Recyclarr.Core.Tests.ServarrApi.MediaManagement;

internal sealed class SonarrV5MediaManagementGatewayTest
{
    [Test, AutoMockData]
    public async Task Update_changes_owned_field_and_round_trips_the_rest(
        [Frozen] SonarrApi.IMediaManagementSettingsApi api,
        SonarrV5MediaManagementGateway sut
    )
    {
        const string serverJson = """
            {
              "id": 1,
              "downloadPropersAndRepacks": "preferAndUpgrade",
              "recycleBin": "/recycle",
              "seasonPackUpgrade": "threshold",
              "seasonPackUpgradeThreshold": 75,
              "skipFreeSpaceCheckWhenGrabbing": true,
              "userRejectedExtensions": ".exe"
            }
            """;
        api.MediamanagementGet()
            .ReturnsForAnyArgs(
                JsonSerializer.Deserialize<SonarrApi.MediaManagementSettingsResource>(
                    serverJson,
                    GlobalJsonSerializerSettings.Services
                )!
            );

        var fetched = await sut.GetMediaManagement(CancellationToken.None);
        fetched.PropersAndRepacks.Should().Be(PropersAndRepacksMode.PreferAndUpgrade);

        await sut.UpdateMediaManagement(
            fetched with
            {
                PropersAndRepacks = PropersAndRepacksMode.DoNotPrefer,
            },
            CancellationToken.None
        );

        var call = api.ReceivedCalls()
            .Single(c =>
                c.GetMethodInfo().Name
                == nameof(SonarrApi.IMediaManagementSettingsApi.MediamanagementPut)
            );
        call.GetArguments()[0].Should().Be("1");
        var sent = JsonSerializer.SerializeToNode(
            call.GetArguments()[1],
            GlobalJsonSerializerSettings.Services
        )!;
        sent["downloadPropersAndRepacks"]!
            .GetValue<string>()
            .Should()
            .BeEquivalentTo("doNotPrefer");
        sent["recycleBin"]!.GetValue<string>().Should().Be("/recycle");
        sent["seasonPackUpgrade"]!.GetValue<string>().Should().BeEquivalentTo("threshold");
        sent["seasonPackUpgradeThreshold"]!.GetValue<double>().Should().Be(75);
        sent["skipFreeSpaceCheckWhenGrabbing"]!.GetValue<bool>().Should().BeTrue();
        sent["userRejectedExtensions"]!.GetValue<string>().Should().Be(".exe");
    }
}
