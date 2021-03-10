using System.Collections.Generic;
using System.Threading.Tasks;

namespace Trash.Radarr.QualityDefinition
{
    public interface IRadarrQualityDefinitionGuideParser
    {
        Task<string> GetMarkdownData();
        IDictionary<RadarrQualityDefinitionType, List<RadarrQualityData>> ParseMarkdown(string markdown);
    }
}
