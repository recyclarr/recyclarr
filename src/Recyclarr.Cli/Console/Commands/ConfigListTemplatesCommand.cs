using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Server;
using Recyclarr.Client.V1;
using Recyclarr.Platform;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.Console.Commands;

[Description("List the templates accepted by `config create`, or the includes for configs.")]
internal class ConfigListTemplatesCommand(
    ServerConnectionFactory connections,
    IAnsiConsole console,
    ILogger log
) : AsyncCommand<ConfigListTemplatesCommand.CliSettings>
{
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    internal class CliSettings : ListCommandSettings
    {
        [CommandOption("-i|--includes")]
        [Description(
            "List templates that may be included in YAML, instead of root templates used with "
                + "`config create`."
        )]
        public bool Includes { get; init; }
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        CliSettings settings,
        CancellationToken ct
    )
    {
        await using var connection = await connections.ConnectAsync(ct);
        var api = connection.ConfigTemplates;

        // Includes have no guide profile, so their Profile is null and its column is omitted.
        IReadOnlyList<Entry> entries = settings.Includes
            ?
            [
                .. (await api.ConfigIncludes(ct))
                    .ContentOrThrow()
                    .Items.Select(i => new Entry(i.Service, i.Id, Profile: null)),
            ]
            :
            [
                .. (await api.ConfigTemplatesGet(ct))
                    .ContentOrThrow()
                    .Items.Select(t => new Entry(t.Service, t.Id, t.ProfileName)),
            ];

        log.Information(
            "Found {SonarrCount} Sonarr and {RadarrCount} Radarr {Kind}",
            entries.Count(e => e.Service == SupportedServices.Sonarr),
            entries.Count(e => e.Service == SupportedServices.Radarr),
            settings.Includes ? "includes" : "templates"
        );

        if (settings.Raw)
        {
            foreach (var entry in entries)
            {
                var fields = new[] { ServiceName(entry.Service), entry.Id, entry.Profile };
                console.WriteRawLine(string.Join('\t', fields.OfType<string>()));
            }

            return (int)ExitStatus.Succeeded;
        }

        console.WriteLine();
        console.WriteLine(
            settings.Includes ? "Reusable include templates:" : "Configuration templates:"
        );
        console.WriteLine();
        WriteTable(entries, withProfile: !settings.Includes);
        console.WriteLine();
        console.WriteLine(
            settings.Includes
                ? "Use these with the `include` property in your config files."
                : "Use these with `recyclarr config create -t <template>`."
        );

        return (int)ExitStatus.Succeeded;
    }

    private void WriteTable(IReadOnlyList<Entry> entries, bool withProfile)
    {
        var table = new Table().AddColumn("Service").AddColumn("ID");
        if (withProfile)
        {
            table.AddColumn("Profile");
        }

        foreach (var entry in entries)
        {
            var cells = new List<IRenderable>
            {
                new Text(entry.Service.ToString()),
                new Markup($"[blue]{Markup.Escape(entry.Id)}[/]"),
            };
            if (withProfile)
            {
                cells.Add(new Text(entry.Profile ?? ""));
            }

            table.AddRow(cells);
        }

        console.Write(table);
    }

    private static string ServiceName(SupportedServices service) =>
        service.ToString().ToLowerInvariant();

    private sealed record Entry(SupportedServices Service, string Id, string? Profile);
}
