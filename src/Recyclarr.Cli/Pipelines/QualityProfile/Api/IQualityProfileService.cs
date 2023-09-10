using Recyclarr.TrashLib.Config;

namespace Recyclarr.Cli.Pipelines.QualityProfile.Api;

public interface IQualityProfileService
{
    Task<IList<QualityProfileDto>> GetQualityProfiles(IServiceConfiguration config);
    Task UpdateQualityProfile(IServiceConfiguration config, QualityProfileDto profile);
    Task<QualityProfileDto> GetSchema(IServiceConfiguration config);
    Task CreateQualityProfile(IServiceConfiguration config, QualityProfileDto profile);
}
