using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.Processors.Sync.Results;

/// <summary>
/// Builds the "Sync Diagnostics" panel: errors, then warnings, each sorted by instance and message.
/// Each instance keeps one prefix color across both sections so its messages are easy to follow.
/// </summary>
internal static class DiagnosticsPanel
{
    private static readonly string[] PrefixColors = ["cyan", "magenta", "blue", "green", "yellow"];

    public static IRenderable? Build(IReadOnlyList<SyncDiagnostic> diagnostics)
    {
        if (diagnostics.Count == 0)
        {
            return null;
        }

        var colors = diagnostics
            .OrderBy(x => x.Severity)
            .Select(x => x.Instance)
            .OfType<string>()
            .Distinct()
            .Select((instance, i) => (instance, PrefixColors[i % PrefixColors.Length]))
            .ToDictionary(x => x.instance, x => x.Item2);

        var sections = new List<IRenderable>();
        AddSection(sections, "Errors", "red", DiagnosticSeverity.Error);
        AddSection(sections, "Warnings", "yellow", DiagnosticSeverity.Warning);

        return new Panel(new Rows(sections))
            .Header("[bold]Sync Diagnostics[/]")
            .Border(BoxBorder.Rounded)
            .Expand();

        void AddSection(
            List<IRenderable> target,
            string header,
            string color,
            DiagnosticSeverity severity
        )
        {
            var entries = diagnostics
                .Where(x => x.Severity == severity)
                .OrderBy(x => x.Instance ?? "", StringComparer.Ordinal)
                .ThenBy(x => x.Message, StringComparer.Ordinal)
                .ToList();

            if (entries.Count == 0)
            {
                return;
            }

            if (target.Count > 0)
            {
                target.Add(new Text(""));
            }

            var grid = new Grid();
            grid.AddColumn(new GridColumn().NoWrap().PadLeft(0).PadRight(1));
            grid.AddColumn(new GridColumn().PadLeft(0).PadRight(0));

            foreach (var entry in entries)
            {
                var prefix = entry.Instance is { } instance
                    ? $"[{colors[instance]}][[{instance.EscapeMarkup()}]][/] "
                    : "";

                grid.AddRow(
                    new Markup($"[{color}]•[/]"),
                    new Markup(prefix + entry.Message.EscapeMarkup())
                );
            }

            target.Add(
                new Rows(
                    new Markup($"[{color}]{header}[/]"),
                    new Markup($"[{color}]{new string('─', header.Length)}[/]"),
                    grid
                )
            );
        }
    }
}
