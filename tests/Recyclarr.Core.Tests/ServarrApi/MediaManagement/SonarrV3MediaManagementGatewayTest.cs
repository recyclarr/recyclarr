using System.Text.Json;
using Recyclarr.Config.Models;
using Recyclarr.Json;
using Recyclarr.ServarrApi.MediaManagement;
using SonarrApi = Recyclarr.Api.Sonarr.V3;

namespace Recyclarr.Core.Tests.ServarrApi.MediaManagement;

internal sealed class SonarrV3MediaManagementGatewayTest
{
    [Test, AutoMockData]
    public async Task Get_returns_domain_data_with_correct_mapping(
        [Frozen] SonarrApi.IMediaManagementConfigApi api,
        SonarrV3MediaManagementGateway sut
    )
    {
        var dto = new SonarrApi.MediaManagementConfigResource
        {
            Id = 5,
            DownloadPropersAndRepacks = SonarrApi.ProperDownloadTypes.DoNotUpgrade,
        };
        api.MediamanagementGet(Arg.Any<CancellationToken>()).Returns(dto);

        var result = await sut.GetMediaManagement(CancellationToken.None);

        result.Id.Should().Be(5);
        result.PropersAndRepacks.Should().Be(PropersAndRepacksMode.DoNotUpgrade);
    }

    [Test, AutoMockData]
    public async Task Update_merges_domain_changes_onto_stashed_dto(
        [Frozen] SonarrApi.IMediaManagementConfigApi api,
        SonarrV3MediaManagementGateway sut
    )
    {
        var originalDto = new SonarrApi.MediaManagementConfigResource
        {
            Id = 1,
            DownloadPropersAndRepacks = SonarrApi.ProperDownloadTypes.PreferAndUpgrade,
            RecycleBin = "/recycle",
        };
        api.MediamanagementGet(Arg.Any<CancellationToken>()).Returns(originalDto);

        // Fetch to populate stashed state
        var fetched = await sut.GetMediaManagement(CancellationToken.None);

        // Update with changed domain data
        var updated = fetched with
        {
            PropersAndRepacks = PropersAndRepacksMode.DoNotPrefer,
        };
        await sut.UpdateMediaManagement(updated, CancellationToken.None);

        await api.Received()
            .MediamanagementPut(
                "1",
                Arg.Is<SonarrApi.MediaManagementConfigResource>(d =>
                    d.DownloadPropersAndRepacks == SonarrApi.ProperDownloadTypes.DoNotPrefer
                    && d.RecycleBin == "/recycle"
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Test, AutoMockData]
    public async Task Update_round_trips_server_fields_recyclarr_does_not_manage(
        [Frozen] SonarrApi.IMediaManagementConfigApi api,
        SonarrV3MediaManagementGateway sut
    )
    {
        const string serverJson = """
            {
              "id": 1,
              "downloadPropersAndRepacks": "preferAndUpgrade",
              "seasonPackUpgrade": "threshold",
              "seasonPackUpgradeThreshold": 75,
              "skipFreeSpaceCheckWhenGrabbing": true,
              "userRejectedExtensions": ".exe"
            }
            """;
        api.MediamanagementGet(default)
            .ReturnsForAnyArgs(
                JsonSerializer.Deserialize<SonarrApi.MediaManagementConfigResource>(
                    serverJson,
                    GlobalJsonSerializerSettings.Services
                )!
            );
        var fetched = await sut.GetMediaManagement(CancellationToken.None);
        await sut.UpdateMediaManagement(
            fetched with
            {
                PropersAndRepacks = PropersAndRepacksMode.DoNotPrefer,
            },
            CancellationToken.None
        );

        var sentDto = api.ReceivedCalls()
            .Single(c =>
                c.GetMethodInfo().Name
                == nameof(SonarrApi.IMediaManagementConfigApi.MediamanagementPut)
            )
            .GetArguments()[1];
        var sent = JsonSerializer.SerializeToNode(sentDto, GlobalJsonSerializerSettings.Services)!;
        // Generated enum serialization casing differs from Sonarr's; Sonarr reads it either way
        sent["seasonPackUpgrade"]!.GetValue<string>().Should().BeEquivalentTo("threshold");
        sent["seasonPackUpgradeThreshold"]!.GetValue<double>().Should().Be(75);
        sent["skipFreeSpaceCheckWhenGrabbing"]!.GetValue<bool>().Should().BeTrue();
        sent["userRejectedExtensions"]!.GetValue<string>().Should().Be(".exe");
    }

    // Fields Sonarr adds after the vendored spec snapshot must survive the read-update cycle
    [Test, AutoMockData]
    public async Task Update_round_trips_server_fields_unknown_to_the_client(
        [Frozen] SonarrApi.IMediaManagementConfigApi api,
        SonarrV3MediaManagementGateway sut
    )
    {
        const string serverJson = """
            {
              "id": 1,
              "downloadPropersAndRepacks": "preferAndUpgrade",
              "futureSetting": { "enabled": true, "limit": 5 }
            }
            """;
        api.MediamanagementGet(default)
            .ReturnsForAnyArgs(
                JsonSerializer.Deserialize<SonarrApi.MediaManagementConfigResource>(
                    serverJson,
                    GlobalJsonSerializerSettings.Services
                )!
            );
        var fetched = await sut.GetMediaManagement(CancellationToken.None);
        await sut.UpdateMediaManagement(
            fetched with
            {
                PropersAndRepacks = PropersAndRepacksMode.DoNotPrefer,
            },
            CancellationToken.None
        );

        var sentDto = api.ReceivedCalls()
            .Single(c =>
                c.GetMethodInfo().Name
                == nameof(SonarrApi.IMediaManagementConfigApi.MediamanagementPut)
            )
            .GetArguments()[1];
        var sent = JsonSerializer.SerializeToNode(sentDto, GlobalJsonSerializerSettings.Services)!;
        sent["futureSetting"]!.ToJsonString().Should().Be("""{"enabled":true,"limit":5}""");
    }
}
