namespace Recyclarr.Servarr.QualityProfile;

public interface IQualityProfileService
{
    Task<IReadOnlyList<QualityProfileData>> GetQualityProfiles(CancellationToken ct);
    Task<QualityProfileData> GetSchema(CancellationToken ct);
    Task<IReadOnlyList<ProfileLanguage>> GetLanguages(CancellationToken ct);
    Task<QualityProfileData> CreateQualityProfile(QualityProfileData profile, CancellationToken ct);
    Task UpdateQualityProfile(QualityProfileData profile, CancellationToken ct);
}
