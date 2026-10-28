using Refit;
using SonarrApi = Recyclarr.Api.Sonarr.V3;

namespace Recyclarr.EndToEndTests;

// Endpoints only the tests call; production contracts declare only what sync uses
internal interface ISonarrCustomFormatByIdApi
{
    [Get("/api/v3/customformat/{id}")]
    Task<SonarrApi.CustomFormatResource> CustomformatGet(int id, CancellationToken ct = default);
}
