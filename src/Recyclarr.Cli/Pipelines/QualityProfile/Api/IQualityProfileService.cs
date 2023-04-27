using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.Cli.Pipelines.QualityProfile.Api;

public interface IQualityProfileService
{
    Task<IList<QualityProfileDto>> GetQualityProfiles(IServiceConfiguration config);
    Task<QualityProfileDto> UpdateQualityProfile(IServiceConfiguration config, QualityProfileDto profile);
}
