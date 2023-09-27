using System.Diagnostics.CodeAnalysis;
using Recyclarr.Common;
using Recyclarr.TrashGuide.MediaNaming;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.Pipelines.MediaNaming;

public class MediaNamingDataLister
{
    private readonly IAnsiConsole _console;
    private readonly IMediaNamingGuideService _guide;

    public MediaNamingDataLister(
        IAnsiConsole console,
        IMediaNamingGuideService guide)
    {
        _console = console;
        _guide = guide;
    }

    public void ListNaming(SupportedServices serviceType)
    {
        switch (serviceType)
        {
            case SupportedServices.Radarr:
                ListRadarrNaming(_guide.GetRadarrNamingData());
                break;

            case SupportedServices.Sonarr:
                ListSonarrNaming(_guide.GetSonarrNamingData());
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, null);
        }
    }

    private void ListRadarrNaming(RadarrMediaNamingData guideData)
    {
        _console.MarkupLine("Media Naming Formats [red](Preview)[/]");

        _console.WriteLine();
        _console.Write(DictionaryToTable("Movie Folder Format", guideData.Folder));
        _console.WriteLine();
        _console.Write(DictionaryToTable("Standard Movie Format", guideData.File));
    }

    private void ListSonarrNaming(SonarrMediaNamingData guideData)
    {
        _console.MarkupLine("Media Naming Formats [red](Preview)[/]");

        _console.WriteLine();
        _console.Write(DictionaryToTable("Season Folder Format", guideData.Season));
        _console.WriteLine();
        _console.Write(DictionaryToTable("Series Folder Format", guideData.Series));
        _console.WriteLine();
        _console.Write(DictionaryToTable("Standard Episode Format", guideData.Episodes.Standard));
        _console.WriteLine();
        _console.Write(DictionaryToTable("Daily Episode Format", guideData.Episodes.Daily));
        _console.WriteLine();
        _console.Write(DictionaryToTable("Anime Episode Format", guideData.Episodes.Anime));
    }

    private static IRenderable DictionaryToTable(string title, IReadOnlyDictionary<string, string> formats)
    {
        var table = new Table()
            .AddColumns("Key", "Format");

        var alternatingColors = new[] {"white", "paleturquoise4"};
        var colorIndex = 0;

        foreach (var (key, value) in formats)
        {
            var color = alternatingColors[colorIndex];
            table.AddRow(
                $"[{color}]{Markup.Escape(TransformKey(key))}[/]",
                $"[{color}]{Markup.Escape(value)}[/]");
            colorIndex = 1 - colorIndex;
        }

        return new Rows(Markup.FromInterpolated($"[orange3]{title}[/]"), table);
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
