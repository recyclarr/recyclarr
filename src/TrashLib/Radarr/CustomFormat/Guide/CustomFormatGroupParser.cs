using System.Collections.ObjectModel;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Common.Extensions;
using TrashLib.Startup;

namespace TrashLib.Radarr.CustomFormat.Guide;

public record CustomFormatGroupItem(string Name, string Anchor);

public class CustomFormatGroupParser
{
    private readonly IAppPaths _paths;
    private static readonly Regex TableRegex = new(@"^\s*\|(.*)\|\s*$");
    private static readonly Regex LinkRegex = new(@"^\[(.+?)\]\(#(.+?)\)$");

    public CustomFormatGroupParser(IAppPaths paths)
    {
        _paths = paths;
    }

    public IDictionary<string, ReadOnlyCollection<CustomFormatGroupItem>> Parse()
    {
        var mdFile = _paths.RepoDirectory
            .SubDirectory("docs")
            .SubDirectory("Radarr")
            .File("Radarr-collection-of-custom-formats.md");

        var columns = new List<List<string>>();

        using var md = mdFile.OpenText();
        while (!md.EndOfStream)
        {
            var rows = ParseTable(md);

            // Pivot the data so that we have lists of columns instead of lists of rows
            // Taken from: https://stackoverflow.com/a/39485441/157971
            columns.AddRange(rows
                .SelectMany(x => x.Select((value, index) => (value, index)))
                .GroupBy(x => x.index, x => x.value)
                .Select(x => x.ToList()));
        }

        return columns.ToDictionary(
            x => x[0],
            x => x.Skip(1).Select(ParseLink).NotNull().ToList().AsReadOnly());
    }

    private static CustomFormatGroupItem? ParseLink(string markdownLink)
    {
        var match = LinkRegex.Match(markdownLink);
        return match.Success ? new CustomFormatGroupItem(match.Groups[1].Value, match.Groups[2].Value) : null;
    }

    private static IEnumerable<List<string>> ParseTable(TextReader stream)
    {
        var tableRows = new List<List<string>>();

        while (true)
        {
            var line = stream.ReadLine();
            if (line is null)
            {
                break;
            }

            if (!line.Any())
            {
                if (tableRows.Any())
                {
                    break;
                }

                continue;
            }

            var match = TableRegex.Match(line);
            if (!match.Success)
            {
                if (tableRows.Any())
                {
                    break;
                }

                continue;
            }

            var tableRow = match.Groups[1].Value;
            var fields = tableRow.Split('|').Select(x => x.Trim()).ToList();
            if (!fields.Any())
            {
                if (tableRows.Any())
                {
                    break;
                }

                continue;
            }

            tableRows.Add(fields);
        }

        return tableRows
            // Filter out the `|---|---|---|` part of the table between the heading & data rows.
            .Where(x => !Regex.IsMatch(x[0], @"^-+$"));
    }
}
