using System.Text.RegularExpressions;
using Common.Extensions;
using Flurl.Http;

namespace TrashLib.Sonarr.QualityDefinition;

internal class SonarrQualityDefinitionGuideParser : ISonarrQualityDefinitionGuideParser
{
    private readonly Regex _regexHeader = new(@"^#+", RegexOptions.Compiled);

    private readonly Regex _regexTableRow =
        new(@"\| *(.*?) *\| *([\d.]+) *\| *([\d.]+) *\|", RegexOptions.Compiled);

    public async Task<string> GetMarkdownData()
    {
        return await
            "https://raw.githubusercontent.com/TRaSH-/Guides/master/docs/Sonarr/Sonarr-Quality-Settings-File-Size.md"
                .GetStringAsync();
    }

    public IDictionary<SonarrQualityDefinitionType, List<SonarrQualityData>> ParseMarkdown(string markdown)
    {
        var results = new Dictionary<SonarrQualityDefinitionType, List<SonarrQualityData>>();
        List<SonarrQualityData>? table = null;

        var reader = new StringReader(markdown);
        for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
        {
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            var match = _regexHeader.Match(line);
            if (match.Success)
            {
                var type = line.ContainsIgnoreCase("anime")
                    ? SonarrQualityDefinitionType.Anime
                    : SonarrQualityDefinitionType.Series;

                table = results.GetOrCreate(type);

                // If we grab a table that isn't empty, that means for whatever reason *another* table
                // in the markdown is trying to modify a previous table's data. For example, maybe there
                // are two "Series" quality tables. That would be a weird edge case, but handle that
                // here just in case.
                if (table.Count > 0)
                {
                    table = null;
                }
            }
            else if (table != null)
            {
                match = _regexTableRow.Match(line);
                if (match.Success)
                {
                    table.Add(new SonarrQualityData
                    {
                        Name = match.Groups[1].Value,
                        Min = match.Groups[2].Value.ToDecimal(),
                        Max = match.Groups[3].Value.ToDecimal()
                    });
                }
            }
        }

        return results;
    }
}
