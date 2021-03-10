using System.Collections.Generic;
using System.Threading.Tasks;

namespace Trash.Sonarr.QualityDefinition
{
    public interface ISonarrQualityDefinitionGuideParser
    {
        Task<string> GetMarkdownData();
        IDictionary<SonarrQualityDefinitionType, List<SonarrQualityData>> ParseMarkdown(string markdown);
    }
}
