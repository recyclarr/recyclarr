using System.Collections.Generic;
using System.Threading.Tasks;
using Trash.Radarr.QualityDefinition.Api.Objects;

namespace Trash.Radarr.QualityDefinition.Api
{
    public interface IQualityDefinitionService
    {
        Task<List<RadarrQualityDefinitionItem>> GetQualityDefinition();
        Task<IList<RadarrQualityDefinitionItem>> UpdateQualityDefinition(IList<RadarrQualityDefinitionItem> newQuality);
    }
}
