namespace TrashLib.Sonarr.QualityDefinition;

public interface ISonarrQualityDefinitionGuideParser
{
    Task<string> GetMarkdownData();
    IDictionary<SonarrQualityDefinitionType, List<SonarrQualityData>> ParseMarkdown(string markdown);
}
