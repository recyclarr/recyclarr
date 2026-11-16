namespace Recyclarr.Servarr.QualityProfile;

/// <summary>
/// The languages a Radarr instance offers for its quality profiles.
/// </summary>
public interface IRadarrLanguageService
{
    Task<IReadOnlyList<ProfileLanguage>> GetLanguages(CancellationToken ct);
}
