namespace TrashLib.Radarr.QualityDefinition;

public interface IRadarrQualityDefinitionGuideParser
{
    Task<string> GetMarkdownData();
    IDictionary<RadarrQualityDefinitionType, List<RadarrQualityData>> ParseMarkdown(string markdown);
}
