using Recyclarr.Sync.Events;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.Processors.Sync;

internal class DiagnosticsRenderer(IAnsiConsole console, SyncEventStorage storage)
{
    private static readonly string[] InstanceColors =
    [
        "cyan",
        "magenta",
        "blue",
        "green",
        "yellow",
    ];

    public void Report()
    {
        var diagnostics = storage.Diagnostics.ToList();
        if (diagnostics.Count == 0)
        {
            return;
        }

        var colorMap = BuildInstanceColorMap(diagnostics);
        var sections = new List<IRenderable>();

        var errors = diagnostics.Where(d => d.Type == DiagnosticType.Error).ToList();
        if (errors.Count > 0)
        {
            sections.Add(BuildSection("Errors", errors, "red", colorMap));
        }

        var warnings = diagnostics
            .Where(d => d.Type is DiagnosticType.Warning or DiagnosticType.Deprecation)
            .ToList();
        if (warnings.Count > 0)
        {
            if (sections.Count > 0)
            {
                sections.Add(new Text(""));
            }

            sections.Add(BuildSection("Warnings", warnings, "yellow", colorMap));
        }

        var panel = new Panel(new Rows(sections))
            .Header("[bold]Sync Diagnostics[/]")
            .Border(BoxBorder.Rounded)
            .Expand();

        console.WriteLine();
        console.Write(panel);
    }

    // Assigns a unique color to each instance name for visual distinction in diagnostic output.
    // Colors cycle if there are more instances than available colors.
    private static Dictionary<string, string> BuildInstanceColorMap(
        List<DiagnosticEvent> diagnostics
    )
    {
        var instances = diagnostics
            .Select(e => e.InstanceName)
            .Where(i => !string.IsNullOrEmpty(i))
            .Distinct()
            .ToList();

        return instances
            .Select((inst, idx) => (inst, color: InstanceColors[idx % InstanceColors.Length]))
            .ToDictionary(x => x.inst!, x => x.color);
    }

    private static Rows BuildSection(
        string header,
        List<DiagnosticEvent> items,
        string color,
        Dictionary<string, string> colorMap
    )
    {
        var grid = new Grid();
        grid.AddColumn(new GridColumn().NoWrap().PadRight(1).PadLeft(0));
        grid.AddColumn(new GridColumn().PadLeft(0).PadRight(0));

        foreach (var item in items.OrderBy(x => x.InstanceName ?? "").ThenBy(x => x.Message))
        {
            var prefix = FormatInstancePrefix(item.InstanceName, colorMap);
            var message = FormatMessage(item);
            grid.AddRow(new Markup($"  [{color}]•[/]"), new Markup($"{prefix} — {message}"));
        }

        return new Rows(
            new Markup($"[{color}]{header}[/]"),
            new Markup($"[{color}]{new string(c: '─', header.Length)}[/]"),
            grid
        );
    }

    private static string FormatInstancePrefix(
        string? instance,
        Dictionary<string, string> colorMap
    )
    {
        if (string.IsNullOrEmpty(instance))
        {
            return "[grey][[global]][/]";
        }

        var instanceColor = colorMap.GetValueOrDefault(instance, "white");
        return $"[{instanceColor}][[{instance.EscapeMarkup()}]][/]";
    }

    private static string FormatMessage(DiagnosticEvent entry)
    {
        var deprecationTag =
            entry.Type == DiagnosticType.Deprecation ? "[darkorange bold][[DEPRECATED]][/] " : "";
        return $"{deprecationTag}{entry.Message.EscapeMarkup()}";
    }
}
