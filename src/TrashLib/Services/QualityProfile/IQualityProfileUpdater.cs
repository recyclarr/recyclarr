using TrashLib.Config.Services;
using TrashLib.Services.Common;
using TrashLib.Services.QualityProfile.Api;

namespace TrashLib.Services.QualityProfile;

public interface IQualityProfileUpdater
{
    Task Process(bool isPreview, IEnumerable<QualityProfileConfig> configs, IEnumerable<QualityGroupConfig> groups);
}
