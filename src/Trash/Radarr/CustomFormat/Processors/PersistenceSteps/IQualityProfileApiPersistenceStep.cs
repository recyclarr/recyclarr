using System.Collections.Generic;
using System.Threading.Tasks;
using Trash.Radarr.CustomFormat.Api;
using Trash.Radarr.CustomFormat.Models;

namespace Trash.Radarr.CustomFormat.Processors.PersistenceSteps
{
    public interface IQualityProfileApiPersistenceStep
    {
        IDictionary<string, List<QualityProfileCustomFormatScoreEntry>> UpdatedScores { get; }
        IReadOnlyCollection<string> InvalidProfileNames { get; }

        Task Process(IQualityProfileService api,
            IDictionary<string, List<QualityProfileCustomFormatScoreEntry>> cfScores);
    }
}
