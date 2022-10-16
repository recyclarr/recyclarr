using TrashLib.Services.CustomFormat.Api;
using TrashLib.Services.CustomFormat.Models;
using TrashLib.Services.QualityProfile.Api;

namespace TrashLib.Services.CustomFormat.Processors.PersistenceSteps;

public interface IQualityProfileApiPersistenceStep
{
    IDictionary<string, List<UpdatedFormatScore>> UpdatedScores { get; }
    IReadOnlyCollection<string> InvalidProfileNames { get; }

    Task Process(IQualityProfileService api,
        IDictionary<string, QualityProfileCustomFormatScoreMapping> cfScores);
}
