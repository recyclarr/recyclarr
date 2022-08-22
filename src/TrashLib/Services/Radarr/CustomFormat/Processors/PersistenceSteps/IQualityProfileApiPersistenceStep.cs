using TrashLib.Services.Radarr.CustomFormat.Api;
using TrashLib.Services.Radarr.CustomFormat.Models;

namespace TrashLib.Services.Radarr.CustomFormat.Processors.PersistenceSteps;

public interface IQualityProfileApiPersistenceStep
{
    IDictionary<string, List<UpdatedFormatScore>> UpdatedScores { get; }
    IReadOnlyCollection<string> InvalidProfileNames { get; }

    Task Process(IQualityProfileService api,
        IDictionary<string, QualityProfileCustomFormatScoreMapping> cfScores);
}
