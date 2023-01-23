using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Pipelines.QualityProfile.Api;

public interface IQualityProfileService
{
    Task<IList<QualityProfileDto>> GetQualityProfiles(IServiceConfiguration config);
    Task<QualityProfileDto> UpdateQualityProfile(IServiceConfiguration config, QualityProfileDto profile);
}
