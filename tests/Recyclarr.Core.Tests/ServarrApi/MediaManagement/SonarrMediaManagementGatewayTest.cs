using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.MediaManagement;

namespace Recyclarr.Core.Tests.ServarrApi.MediaManagement;

internal sealed class SonarrMediaManagementGatewayTest
{
    [Test, AutoMockData]
    public async Task Get_returns_domain_data_with_correct_mapping(
        [Frozen] IMediaManagementApiService api,
        SonarrMediaManagementGateway sut
    )
    {
        var dto = new MediaManagementDto
        {
            Id = 5,
            DownloadPropersAndRepacks = PropersAndRepacksMode.DoNotUpgrade,
        };
        api.GetMediaManagement(Arg.Any<CancellationToken>()).Returns(dto);

        var result = await sut.GetMediaManagement(CancellationToken.None);

        result.Id.Should().Be(5);
        result.PropersAndRepacks.Should().Be(PropersAndRepacksMode.DoNotUpgrade);
    }

    [Test, AutoMockData]
    public async Task Update_merges_domain_changes_onto_stashed_dto(
        [Frozen] IMediaManagementApiService api,
        SonarrMediaManagementGateway sut
    )
    {
        var originalDto = new MediaManagementDto
        {
            Id = 1,
            DownloadPropersAndRepacks = PropersAndRepacksMode.PreferAndUpgrade,
        };
        originalDto.ExtraJson["someField"] = "preserved";
        api.GetMediaManagement(Arg.Any<CancellationToken>()).Returns(originalDto);

        // Fetch to populate stashed state
        var fetched = await sut.GetMediaManagement(CancellationToken.None);

        // Update with changed domain data
        var updated = fetched with
        {
            PropersAndRepacks = PropersAndRepacksMode.DoNotPrefer,
        };
        await sut.UpdateMediaManagement(updated, CancellationToken.None);

        await api.Received()
            .UpdateMediaManagement(
                Arg.Is<MediaManagementDto>(d =>
                    d.DownloadPropersAndRepacks == PropersAndRepacksMode.DoNotPrefer
                    && d.ExtraJson.ContainsKey("someField")
                ),
                Arg.Any<CancellationToken>()
            );
    }
}
