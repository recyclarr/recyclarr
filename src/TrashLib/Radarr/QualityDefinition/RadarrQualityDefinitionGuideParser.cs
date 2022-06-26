using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Common.Extensions;

namespace TrashLib.Radarr.QualityDefinition;

internal class RadarrQualityDefinitionGuideParser : IRadarrQualityDefinitionGuideParser
{
    private readonly IAppPaths _paths;
    private readonly Regex _regexHeader = new(@"^#+", RegexOptions.Compiled);

    private readonly Regex _regexTableRow =
        new(@"\| *(.*?) *\| *([\d.]+) *\| *([\d.]+) *\|", RegexOptions.Compiled);

    public RadarrQualityDefinitionGuideParser(IAppPaths paths)
    {
        _paths = paths;
    }

    public async Task<string> GetMarkdownData()
    {
        var repoDir = _paths.RepoDirectory;
        var file = repoDir
            .SubDirectory("docs")
            .SubDirectory("Radarr")
            .File("Radarr-Quality-Settings-File-Size.md").OpenText();

        return await file.ReadToEndAsync();
    }

    public IDictionary<RadarrQualityDefinitionType, List<RadarrQualityData>> ParseMarkdown(string markdown)
    {
        var results = new Dictionary<RadarrQualityDefinitionType, List<RadarrQualityData>>();
        List<RadarrQualityData>? table = null;

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
                // todo: hard-coded for now since there's only one supported right now.
                var type = RadarrQualityDefinitionType.Movie;
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
                    table.Add(new RadarrQualityData
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
