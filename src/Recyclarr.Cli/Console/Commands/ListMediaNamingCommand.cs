using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Processors;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.TrashGuide;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("List media naming formats in the guide for a particular service.")]
internal class ListMediaNamingCommand(
    ILogger log,
    IAnsiConsole console,
    MediaNamingResourceQuery guide,
    ProviderProgressHandler providerProgressHandler
) : AsyncCommand<ListMediaNamingCommand.CliSettings>
{
    [UsedImplicitly]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    internal class CliSettings : ListCommandSettings
    {
        [CommandArgument(0, "<service_type>")]
        [EnumDescription<SupportedServices>("The service type to obtain information about.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public SupportedServices Service { get; init; }
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CliSettings settings,
        CancellationToken ct
    )
    {
        await providerProgressHandler.InitializeProvidersAsync(settings.Raw, ct);

        switch (settings.Service)
        {
            case SupportedServices.Radarr:
                ListRadarrNaming(settings.Raw);
                break;

            case SupportedServices.Sonarr:
                ListSonarrNaming(settings.Raw);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(settings),
                    settings.Service,
                    "Unsupported service type"
                );
        }

        return (int)ExitStatus.Succeeded;
    }

    private void ListRadarrNaming(bool raw)
    {
        var guideData = guide.GetRadarr();

        log.Debug(
            "Listing Radarr naming formats: {FolderCount} folder, {FileCount} file",
            guideData.Folder.Count,
            guideData.File.Count
        );

        if (raw)
        {
            OutputRawRadarr("movie_folder", guideData.Folder);
            OutputRawRadarr("standard_movie", guideData.File);
            return;
        }

        console.MarkupLine("[orange3]Media Naming Formats[/] [red](Preview)[/]");

        console.WriteLine();
        console.Write(DictionaryToTableRadarr("Movie Folder Format", guideData.Folder));
        console.WriteLine();
        console.Write(DictionaryToTableRadarr("Standard Movie Format", guideData.File));
    }

    private void ListSonarrNaming(bool raw)
    {
        var guideData = guide.GetSonarr();

        log.Debug(
            "Listing Sonarr naming formats: {SeasonCount} season, {SeriesCount} series, "
                + "{StandardCount} standard, {DailyCount} daily, {AnimeCount} anime",
            guideData.Season.Count,
            guideData.Series.Count,
            guideData.Episodes.Standard.Count,
            guideData.Episodes.Daily.Count,
            guideData.Episodes.Anime.Count
        );

        if (raw)
        {
            OutputRawSonarr("season_folder", guideData.Season);
            OutputRawSonarr("series_folder", guideData.Series);
            OutputRawSonarr("standard_episode", guideData.Episodes.Standard);
            OutputRawSonarr("daily_episode", guideData.Episodes.Daily);
            OutputRawSonarr("anime_episode", guideData.Episodes.Anime);
            return;
        }

        console.MarkupLine("[orange3]Media Naming Formats[/] [red](Preview)[/]");

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

    private void OutputRawRadarr(string formatType, IReadOnlyDictionary<string, string> formats)
    {
        foreach (var (key, value) in formats)
        {
            console.WriteLine($"{formatType}\t{TransformKey(key)}\t{value}");
        }
    }

    private void OutputRawSonarr(string formatType, IReadOnlyDictionary<string, string> formats)
    {
        foreach (var (key, value) in formats)
        {
            var split = key.Split(':');
            var version = split.Length > 1 ? $"v{split[1]}" : "All";
            console.WriteLine($"{formatType}\t{split[0]}\t{version}\t{value}");
        }
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
            Markup.FromInterpolated(CultureInfo.InvariantCulture, $"[grey]{title}[/]"),
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
            Markup.FromInterpolated(CultureInfo.InvariantCulture, $"[grey]{title}[/]"),
            table
        );
    }

    private static string TransformKey(string key)
    {
        var split = key.Split(':');
        return split.Length > 1 ? $"{split[0]} (v{split[1]})" : key;
    }
}
