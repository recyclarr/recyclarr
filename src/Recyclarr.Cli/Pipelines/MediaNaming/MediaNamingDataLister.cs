using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.MediaNaming;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.MediaNaming;

public class MediaNamingDataLister(IAnsiConsole console, IMediaNamingGuideService guide)
{
    public void ListNaming(SupportedServices serviceType)
    {
        switch (serviceType)
        {
            case SupportedServices.Radarr:
                ListRadarrNaming(guide.GetRadarrNamingData());
                break;

            case SupportedServices.Sonarr:
                ListSonarrNaming(guide.GetSonarrNamingData());
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, null);
        }
    }

    private void ListRadarrNaming(RadarrMediaNamingData guideData)
    {
        console.MarkupLine("Media Naming Formats [red](Preview)[/]");

        console.WriteLine();
        console.Write(DictionaryToTableRadarr("Movie Folder Format", guideData.Folder));
        console.WriteLine();
        console.Write(DictionaryToTableRadarr("Standard Movie Format", guideData.File));
    }

    private void ListSonarrNaming(SonarrMediaNamingData guideData)
    {
        console.MarkupLine("Media Naming Formats [red](Preview)[/]");

        console.WriteLine();
        console.Write(DictionaryToTableSonarr("Season Folder Format", guideData.Season));
        console.WriteLine();
        console.Write(DictionaryToTableSonarr("Series Folder Format", guideData.Series));
        console.WriteLine();
        console.Write(
            DictionaryToTableSonarr("Standard Episode Format", guideData.Episodes.Standard)
        );
        console.WriteLine();
        console.Write(DictionaryToTableSonarr("Daily Episode Format", guideData.Episodes.Daily));
        console.WriteLine();
        console.Write(DictionaryToTableSonarr("Anime Episode Format", guideData.Episodes.Anime));
    }

    private static Rows DictionaryToTableRadarr(
        string title,
        IReadOnlyDictionary<string, string> formats
    )
    {
        var table = new Table().AddColumns("Key", "Format");

        var alternatingColors = new[] { "white", "paleturquoise4" };
        var colorIndex = 0;

        foreach (var (key, value) in formats)
        {
            var color = alternatingColors[colorIndex];
            table.AddRow(
                $"[{color}]{Markup.Escape(TransformKey(key))}[/]",
                $"[{color}]{Markup.Escape(value)}[/]"
            );
            colorIndex = 1 - colorIndex;
        }

        return new Rows(
            Markup.FromInterpolated(CultureInfo.InvariantCulture, $"[orange3]{title}[/]"),
            table
        );
    }

    private static Rows DictionaryToTableSonarr(
        string title,
        IReadOnlyDictionary<string, string> formats
    )
    {
        var table = new Table().AddColumns("Key", "Sonarr Version", "Format");

        var alternatingColors = new[] { "white", "paleturquoise4" };
        var colorIndex = 0;

        foreach (var (key, value) in formats)
        {
            var split = key.Split(':');
            var version = split switch
            {
                { Length: 1 } => "All",
                _ => $"v{split[1]}",
            };

            var color = alternatingColors[colorIndex];
            table.AddRow(
                $"[{color}]{Markup.Escape(split[0])}[/]",
                $"[{color}]{Markup.Escape(version)}[/]",
                $"[{color}]{Markup.Escape(value)}[/]"
            );
            colorIndex = 1 - colorIndex;
        }

        return new Rows(
            Markup.FromInterpolated(CultureInfo.InvariantCulture, $"[orange3]{title}[/]"),
            table
        );
    }

    [SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
    private static string TransformKey(string key)
    {
        var split = key.Split(':');
        if (split.Length > 1)
        {
            return $"{split[0]} (v{split[1]})";
        }

        return key;
    }
}
