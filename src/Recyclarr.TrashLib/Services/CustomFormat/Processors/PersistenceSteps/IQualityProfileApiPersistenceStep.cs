using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Services.CustomFormat.Models;

namespace Recyclarr.TrashLib.Services.CustomFormat.Processors.PersistenceSteps;

public interface IQualityProfileApiPersistenceStep
{
    IDictionary<string, List<UpdatedFormatScore>> UpdatedScores { get; }
    IReadOnlyCollection<string> InvalidProfileNames { get; }

    Task Process(
        IServiceConfiguration config,
        IDictionary<string, QualityProfileCustomFormatScoreMapping> cfScores);
}
