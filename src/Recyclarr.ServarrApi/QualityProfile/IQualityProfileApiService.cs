namespace Recyclarr.ServarrApi.QualityProfile;

public interface IQualityProfileApiService
{
    Task<IList<QualityProfileDto>> GetQualityProfiles();
    Task UpdateQualityProfile(QualityProfileDto profile);
    Task<QualityProfileDto> GetSchema();
    Task CreateQualityProfile(QualityProfileDto profile);
}
