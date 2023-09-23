using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.Dto;

namespace Recyclarr.ServarrApi.Services;

public interface IQualityProfileService
{
    Task<IList<QualityProfileDto>> GetQualityProfiles(IServiceConfiguration config);
    Task UpdateQualityProfile(IServiceConfiguration config, QualityProfileDto profile);
    Task<QualityProfileDto> GetSchema(IServiceConfiguration config);
    Task CreateQualityProfile(IServiceConfiguration config, QualityProfileDto profile);
}
