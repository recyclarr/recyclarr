using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.Console.Widgets;

internal sealed class DiagnosticPanel(string title)
{
    private static readonly string[] PrefixColors = ["cyan", "magenta", "blue", "green", "yellow"];

    private readonly List<DiagnosticEntry> _errors = [];
    private readonly List<DiagnosticEntry> _warnings = [];
    private readonly List<DiagnosticEntry> _deprecations = [];

    public void AddError(string? prefix, string message) =>
        _errors.Add(new DiagnosticEntry(message, prefix));

    public void AddWarning(string? prefix, string message) =>
        _warnings.Add(new DiagnosticEntry(message, prefix));

    public void AddDeprecation(string? prefix, string message) =>
        _deprecations.Add(new DiagnosticEntry(message, prefix));

    public void Render(IAnsiConsole console)
    {
        var allEntries = _errors.Concat(_warnings).Concat(_deprecations);
        var colorMap = BuildPrefixColorMap(allEntries);

        var sections = new List<IRenderable>();

        AddSection(sections, "Errors", "red", _errors, colorMap);
        AddSection(sections, "Warnings", "yellow", _warnings, colorMap);
        AddSection(sections, "Deprecations", "darkorange", _deprecations, colorMap);

        if (sections.Count == 0)
        {
            return;
        }

        // Separate sections with blank lines
        var rows = new List<IRenderable>();
        for (var i = 0; i < sections.Count; i++)
        {
            if (i > 0)
            {
                rows.Add(new Text(""));
            }

            rows.Add(sections[i]);
        }

        var panel = new Panel(new Rows(rows))
            .Header($"[bold]{title.EscapeMarkup()}[/]")
            .Border(BoxBorder.Rounded)
            .Expand();

        console.WriteLine();
        console.Write(panel);
        console.WriteLine();
    }

    private static void AddSection(
        List<IRenderable> sections,
        string header,
        string color,
        List<DiagnosticEntry> entries,
        Dictionary<string, string> colorMap
    )
    {
        if (entries.Count == 0)
        {
            return;
        }

        var grid = new Grid();
        grid.AddColumn(new GridColumn().NoWrap().PadRight(1).PadLeft(0));
        grid.AddColumn(new GridColumn().PadLeft(0).PadRight(0));

        foreach (var entry in entries.OrderBy(e => e.Prefix ?? "").ThenBy(e => e.Message))
        {
            var prefix = FormatPrefix(entry.Prefix, colorMap);
            var message = $"{prefix}{entry.Message.EscapeMarkup()}";

            grid.AddRow(new Markup($"[{color}]•[/]"), new Markup(message));
        }

        sections.Add(
            new Rows(
                new Markup($"[{color}]{header}[/]"),
                new Markup($"[{color}]{new string(c: '─', header.Length)}[/]"),
                grid
            )
        );
    }

    private static string FormatPrefix(string? prefix, Dictionary<string, string> colorMap)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return "";
        }

        var color = colorMap.GetValueOrDefault(prefix, "white");
        return $"[{color}][[{prefix.EscapeMarkup()}]][/] ";
    }

    private static Dictionary<string, string> BuildPrefixColorMap(
        IEnumerable<DiagnosticEntry> entries
    )
    {
        var prefixes = entries
            .Select(e => e.Prefix)
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct()
            .ToList();

        return prefixes
            .Select((p, idx) => (p, color: PrefixColors[idx % PrefixColors.Length]))
            .ToDictionary(x => x.p!, x => x.color);
    }
}
