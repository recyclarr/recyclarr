using System.Collections.Generic;
using System.Threading.Tasks;
using TrashLib.Radarr.CustomFormat.Api;
using TrashLib.Radarr.CustomFormat.Models;

namespace TrashLib.Radarr.CustomFormat.Processors.PersistenceSteps
{
    public interface IQualityProfileApiPersistenceStep
    {
        IDictionary<string, List<UpdatedFormatScore>> UpdatedScores { get; }
        IReadOnlyCollection<string> InvalidProfileNames { get; }

        Task Process(IQualityProfileService api,
            IDictionary<string, QualityProfileCustomFormatScoreMapping> cfScores);
    }
}
