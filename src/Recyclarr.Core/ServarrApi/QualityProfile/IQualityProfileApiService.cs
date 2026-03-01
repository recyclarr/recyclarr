namespace Recyclarr.ServarrApi.QualityProfile;

public interface IQualityProfileApiService
{
    Task<IList<ServiceQualityProfileData>> GetQualityProfiles(CancellationToken ct);
    Task UpdateQualityProfile(ServiceQualityProfileData profile, CancellationToken ct);
    Task<ServiceQualityProfileData> GetSchema(CancellationToken ct);

    Task<ServiceQualityProfileData> CreateQualityProfile(
        ServiceQualityProfileData profile,
        CancellationToken ct
    );

    Task<IList<ServiceProfileLanguage>> GetLanguages(CancellationToken ct);
}
