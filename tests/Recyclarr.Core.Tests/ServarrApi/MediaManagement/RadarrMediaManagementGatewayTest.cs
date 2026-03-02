using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.MediaManagement;
using RadarrApi = Recyclarr.Api.Radarr;

namespace Recyclarr.Core.Tests.ServarrApi.MediaManagement;

internal sealed class RadarrMediaManagementGatewayTest
{
    [Test, AutoMockData]
    public async Task Get_returns_domain_data_with_correct_mapping(
        [Frozen] RadarrApi.IMediaManagementConfigApi api,
        RadarrMediaManagementGateway sut
    )
    {
        var dto = new RadarrApi.MediaManagementConfigResource
        {
            Id = 5,
            DownloadPropersAndRepacks = RadarrApi.ProperDownloadTypes.DoNotUpgrade,
        };
        api.MediamanagementGet(Arg.Any<CancellationToken>()).Returns(dto);

        var result = await sut.GetMediaManagement(CancellationToken.None);

        result.Id.Should().Be(5);
        result.PropersAndRepacks.Should().Be(PropersAndRepacksMode.DoNotUpgrade);
    }

    [Test, AutoMockData]
    public async Task Update_merges_domain_changes_onto_stashed_dto(
        [Frozen] RadarrApi.IMediaManagementConfigApi api,
        RadarrMediaManagementGateway sut
    )
    {
        var originalDto = new RadarrApi.MediaManagementConfigResource
        {
            Id = 1,
            DownloadPropersAndRepacks = RadarrApi.ProperDownloadTypes.PreferAndUpgrade,
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
                Arg.Is<RadarrApi.MediaManagementConfigResource>(d =>
                    d.DownloadPropersAndRepacks == RadarrApi.ProperDownloadTypes.DoNotPrefer
                    && d.RecycleBin == "/recycle"
                ),
                Arg.Any<CancellationToken>()
            );
    }
}
