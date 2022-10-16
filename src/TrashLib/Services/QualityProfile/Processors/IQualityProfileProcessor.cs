using TrashLib.Config.Services;
using TrashLib.Services.Common;
using TrashLib.Services.QualityProfile.Api;

namespace TrashLib.Services.QualityProfile.Processors;

internal interface IQualityProfileProcessor
{

    //IReadOnlyCollection<ProcessedConfigData> ConfigData { get; }

    Task BuildQualityProfileDataAsync(IEnumerable<QualityGroupConfig> qualityGroupConfig, IEnumerable<QualityProfileConfig> qualityProfileConfig, IQualityProfileService qualityProfileService);

    void Reset();
}
