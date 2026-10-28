using Recyclarr.Core.TestLibrary;
using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Core.Tests.ServarrApi.MediaNaming;

internal sealed class SonarrV5NamingGatewayTest : ServarrHttpTestFixture
{
    private const string Path = "/api/v5/settings/naming";

    [Test]
    public async Task Fetch_maps_and_update_changes_only_owned_fields()
    {
        Respond(
            HttpMethod.Get,
            Path,
            """
            {
              "id": 4,
              "renameEpisodes": false,
              "seasonFolderFormat": "Season {season}",
              "replaceIllegalCharacters": true,
              "colonReplacementFormat": "futureFormat"
            }
            """
        );
        var sut = ResolveGateway<SonarrV5NamingGateway>(NewConfig.Sonarr());

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

        SentBody(HttpMethod.Put, $"{Path}/4")
            .ToJsonString()
            .Should()
            .Be(
                """{"id":4,"renameEpisodes":true,"seasonFolderFormat":"Season {season:00}","replaceIllegalCharacters":true,"colonReplacementFormat":"futureFormat"}"""
            );
    }
}
