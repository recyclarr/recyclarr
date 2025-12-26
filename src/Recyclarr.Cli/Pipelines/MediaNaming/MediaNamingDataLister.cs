using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.TrashGuide;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.MediaNaming;

internal class MediaNamingDataLister(
    ILogger log,
    IAnsiConsole console,
    MediaNamingResourceQuery guide
)
{
    public void ListNaming(SupportedServices serviceType)
    {
        switch (serviceType)
        {
            case SupportedServices.Radarr:
                ListRadarrNaming(guide.GetRadarr());
                break;

            case SupportedServices.Sonarr:
                ListSonarrNaming(guide.GetSonarr());
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, null);
        }
    }

    private void ListRadarrNaming(RadarrMediaNamingResource guideData)
    {
        log.Debug(
            "Listing Radarr naming formats: {FolderCount} folder, {FileCount} file",
            guideData.Folder.Count,
            guideData.File.Count
        );

        console.MarkupLine("Media Naming Formats [red](Preview)[/]");

        console.WriteLine();
        console.Write(DictionaryToTableRadarr("Movie Folder Format", guideData.Folder));
        console.WriteLine();
        console.Write(DictionaryToTableRadarr("Standard Movie Format", guideData.File));
    }

    private void ListSonarrNaming(SonarrMediaNamingResource guideData)
    {
        log.Debug(
            "Listing Sonarr naming formats: {SeasonCount} season, {SeriesCount} series, "
                + "{StandardCount} standard, {DailyCount} daily, {AnimeCount} anime",
            guideData.Season.Count,
            guideData.Series.Count,
            guideData.Episodes.Standard.Count,
            guideData.Episodes.Daily.Count,
            guideData.Episodes.Anime.Count
        );

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
