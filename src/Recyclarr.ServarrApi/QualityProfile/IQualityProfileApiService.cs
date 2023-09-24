using Recyclarr.Config.Models;

namespace Recyclarr.ServarrApi.QualityProfile;

public interface IQualityProfileApiService
{
    Task<IList<QualityProfileDto>> GetQualityProfiles(IServiceConfiguration config);
    Task UpdateQualityProfile(IServiceConfiguration config, QualityProfileDto profile);
    Task<QualityProfileDto> GetSchema(IServiceConfiguration config);
    Task CreateQualityProfile(IServiceConfiguration config, QualityProfileDto profile);
}
