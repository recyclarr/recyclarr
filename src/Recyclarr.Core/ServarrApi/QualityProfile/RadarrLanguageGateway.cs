using Recyclarr.Servarr.QualityProfile;
using RadarrApi = Recyclarr.Api.Radarr;

namespace Recyclarr.ServarrApi.QualityProfile;

internal class RadarrLanguageGateway(RadarrApi.ILanguageApi languageApi) : IRadarrLanguageService
{
    public async Task<IReadOnlyList<ProfileLanguage>> GetLanguages(CancellationToken ct)
    {
        var dtos = await languageApi.LanguageGet(ct);
        return dtos.Select(RadarrQualityProfileMapper.ToDomain).ToList();
    }
}
