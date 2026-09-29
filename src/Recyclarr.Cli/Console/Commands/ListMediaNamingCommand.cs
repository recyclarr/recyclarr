using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Server;
using Recyclarr.Client.V1;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("List media naming formats in the guide for a particular service.")]
internal class ListMediaNamingCommand(
    ILogger log,
    IAnsiConsole console,
    ServerConnectionFactory connections
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

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        CliSettings settings,
        CancellationToken ct
    )
    {
        await using var connection = await connections.ConnectAsync(ct);
        var response = await connection.Guide.Naming(settings.Service, ct);

        IReadOnlyList<(string Type, string Title, IReadOnlyList<GuideNamingFormatResponse>)> lists =
            response.ContentOrThrow() switch
            {
                GuideNamingResponseRadarr radarr =>
                [
                    ("movie_folder", "Movie Folder Format", radarr.MovieFolder),
                    ("standard_movie", "Standard Movie Format", radarr.StandardMovie),
                ],
                GuideNamingResponseSonarr sonarr =>
                [
                    ("season_folder", "Season Folder Format", sonarr.SeasonFolder),
                    ("series_folder", "Series Folder Format", sonarr.SeriesFolder),
                    ("standard_episode", "Standard Episode Format", sonarr.StandardEpisode),
                    ("daily_episode", "Daily Episode Format", sonarr.DailyEpisode),
                    ("anime_episode", "Anime Episode Format", sonarr.AnimeEpisode),
                ],
                _ => throw new InvalidOperationException(
                    $"The server returned naming data for an unknown service: {settings.Service}"
                ),
            };

        log.Debug(
            "Listing {Service} naming formats: {Counts}",
            settings.Service,
            lists.Select(x => $"{x.Type}={x.Item3.Count}")
        );

        if (settings.Raw)
        {
            foreach (var (type, _, formats) in lists)
            {
                foreach (var format in formats)
                {
                    // Raw columns differ by service to keep existing scripts working.
                    console.WriteRawLine(
                        settings.Service == SupportedServices.Radarr
                            ? $"{type}\t{RadarrRawKey(format)}\t{format.Format}"
                            : $"{type}\t{format.Key}\t{VersionLabel(format)}\t{format.Format}"
                    );
                }
            }

            return (int)ExitStatus.Succeeded;
        }

        console.MarkupLine("[orange3]Media Naming Formats[/] [red](Preview)[/]");
        foreach (var (_, title, formats) in lists)
        {
            console.WriteLine();
            console.Write(ToTable(title, formats));
        }

        return (int)ExitStatus.Succeeded;
    }

    private static string RadarrRawKey(GuideNamingFormatResponse format) =>
        format.Version is null ? format.Key : $"{format.Key} (v{format.Version})";

    private static string VersionLabel(GuideNamingFormatResponse format) =>
        format.Version is null ? "All" : $"v{format.Version}";

    private static Rows ToTable(string title, IEnumerable<GuideNamingFormatResponse> formats)
    {
        var table = new Table().AddColumns("Key", "Version", "Format");

        var alternatingColors = new[] { "white", "paleturquoise4" };
        var colorIndex = 0;

        foreach (var format in formats)
        {
            var color = alternatingColors[colorIndex];
            table.AddRow(
                $"[{color}]{Markup.Escape(format.Key)}[/]",
                $"[{color}]{Markup.Escape(VersionLabel(format))}[/]",
                $"[{color}]{Markup.Escape(format.Format)}[/]"
            );
            colorIndex = 1 - colorIndex;
        }

        return new Rows(new Markup($"[grey]{Markup.Escape(title)}[/]"), table);
    }
}
