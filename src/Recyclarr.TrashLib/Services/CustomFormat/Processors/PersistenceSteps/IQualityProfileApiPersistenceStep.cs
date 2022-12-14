using Recyclarr.TrashLib.Services.CustomFormat.Api;
using Recyclarr.TrashLib.Services.CustomFormat.Models;

namespace Recyclarr.TrashLib.Services.CustomFormat.Processors.PersistenceSteps;

public interface IQualityProfileApiPersistenceStep
{
    IDictionary<string, List<UpdatedFormatScore>> UpdatedScores { get; }
    IReadOnlyCollection<string> InvalidProfileNames { get; }

    Task Process(IQualityProfileService api,
        IDictionary<string, QualityProfileCustomFormatScoreMapping> cfScores);
}
