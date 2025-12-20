namespace Recyclarr.ServarrApi.QualityProfile;

public interface IQualityProfileApiService
{
    Task<IList<QualityProfileDto>> GetQualityProfiles(CancellationToken ct);
    Task UpdateQualityProfile(QualityProfileDto profile, CancellationToken ct);
    Task<QualityProfileDto> GetSchema(CancellationToken ct);
    Task CreateQualityProfile(QualityProfileDto profile, CancellationToken ct);
    Task<IList<ProfileLanguageDto>> GetLanguages(CancellationToken ct);
}
