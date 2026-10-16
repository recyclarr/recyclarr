using Recyclarr.ServarrApi.MediaNaming;
using SonarrApi = Recyclarr.Api.Sonarr.V5;

namespace Recyclarr.Core.Tests.ServarrApi.MediaNaming;

internal sealed class SonarrV5NamingGatewayTest
{
    [Test, AutoMockData]
    public async Task Fetch_maps_and_update_changes_only_owned_fields(
        [Frozen] SonarrApi.INamingSettingsApi api,
        SonarrV5NamingGateway sut
    )
    {
        api.NamingGet()
            .ReturnsForAnyArgs(
                new SonarrApi.NamingSettingsResource
                {
                    Id = 4,
                    RenameEpisodes = false,
                    SeasonFolderFormat = "Season {season}",
                    ReplaceIllegalCharacters = true,
                }
            );

        var fetched = await sut.GetNaming(CancellationToken.None);
        fetched.RenameEpisodes.Should().BeFalse();
        fetched.SeasonFolderFormat.Should().Be("Season {season}");

        await sut.UpdateNaming(
            fetched with
            {
                RenameEpisodes = true,
                SeasonFolderFormat = "Season {season:00}",
            },
            CancellationToken.None
        );

        var call = api.ReceivedCalls()
            .Single(c => c.GetMethodInfo().Name == nameof(SonarrApi.INamingSettingsApi.NamingPut));
        call.GetArguments()[0].Should().Be("4");
        call.GetArguments()[1]
            .Should()
            .BeEquivalentTo(
                new
                {
                    RenameEpisodes = true,
                    SeasonFolderFormat = "Season {season:00}",
                    ReplaceIllegalCharacters = true,
                }
            );
    }
}
