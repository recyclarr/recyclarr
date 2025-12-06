using System.Globalization;
using Spectre.Console;
using Spectre.Console.Rendering;

// Parse CLI args
var scenario = args.Length > 0 ? args[0].ToLowerInvariant() : "error";
var simulateErrors = scenario is "error" or "error-global";
var simulateGlobalError = scenario == "error-global";

if (scenario is not ("success" or "error" or "error-global"))
{
    AnsiConsole.MarkupLine("[yellow]Usage:[/] dotnet run -- [success|error|error-global]");
    AnsiConsole.MarkupLine("  [grey]success[/]      - all pipelines succeed, no diagnostics");
    AnsiConsole.MarkupLine(
        "  [grey]error[/]        - simulate instance errors and warnings (default)"
    );
    AnsiConsole.MarkupLine("  [grey]error-global[/] - simulate a global error");
    return;
}

// Spinner frames for the "dots" spinner
var spinnerFrames = new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
var frameIndex = 0;

// Simulated instance data
var instances = new List<InstanceState>
{
    new("sonarr-main", SupportedPipelines.All),
    new("radarr-4k", SupportedPipelines.All),
    new("radarr-anime", SupportedPipelines.CfAndQp),
};

// Simulated diagnostics collector
var diagnostics = new DiagnosticsCollector();

// Add global error if requested
if (simulateGlobalError)
{
    diagnostics.AddError(
        "Failed to connect to TRaSH Guides repository. Check your network connection."
    );
}

// Check for global errors before running pipelines
if (diagnostics.Errors.Any(e => e.Instance is null))
{
    // Global error - skip sync entirely, show diagnostics immediately
    RenderDiagnosticReport(diagnostics);
}
else
{
    // Print legend (simplified - no abbreviations needed with column headers)
    AnsiConsole.MarkupLine(
        "[grey]Legend:[/] [green]✓[/] ok [grey]·[/] [red]✗[/] failed [grey]·[/] [red]-[/] error [grey]·[/] [grey]--[/] skipped"
    );
    AnsiConsole.WriteLine();

    // Run the sync simulation with live display
    await AnsiConsole
        .Live(BuildDisplay(instances, spinnerFrames[0]))
        .AutoClear(false)
        .StartAsync(async ctx =>
        {
            foreach (var instance in instances)
            {
                instance.Status = InstanceStatus.Running;
                await SimulateInstanceSync(
                    instance,
                    diagnostics,
                    ctx,
                    () =>
                    {
                        frameIndex = (frameIndex + 1) % spinnerFrames.Length;
                        return spinnerFrames[frameIndex];
                    }
                );
            }
        });

    // Blank line before report
    AnsiConsole.WriteLine();

    // Render final diagnostic report
    RenderDiagnosticReport(diagnostics);
}

return;

async Task SimulateInstanceSync(
    InstanceState instance,
    DiagnosticsCollector diag,
    LiveDisplayContext ctx,
    Func<string> getSpinnerFrame
)
{
    var pipelines = new[] { "CF", "QP", "QS", "MN" };

    foreach (var pipeline in pipelines)
    {
        if (!instance.SupportsPipeline(pipeline))
        {
            instance.PipelineResults[pipeline] = new PipelineResult(PipelineStatus.Skipped, null);
            continue;
        }

        instance.PipelineResults[pipeline] = new PipelineResult(PipelineStatus.Running, null);

        // Animate spinner while "working"
        var workTime = Random.Shared.Next(250, 1000);
        var elapsed = 0;
        while (elapsed < workTime)
        {
            ctx.UpdateTarget(BuildDisplay(instances, getSpinnerFrame()));
            await Task.Delay(80);
            elapsed += 80;
        }

        // Simulate result
        var (status, count) = SimulatePipelineResult(instance.Name, pipeline, diag, simulateErrors);
        instance.PipelineResults[pipeline] = new PipelineResult(status, count);
        ctx.UpdateTarget(BuildDisplay(instances, getSpinnerFrame()));
    }

    // Determine overall instance status
    instance.Status = instance.PipelineResults.Values.Any(r => r.Status == PipelineStatus.Failed)
        ? InstanceStatus.Failed
        : InstanceStatus.Succeeded;

    ctx.UpdateTarget(BuildDisplay(instances, getSpinnerFrame()));
}

(PipelineStatus, int?) SimulatePipelineResult(
    string instanceName,
    string pipeline,
    DiagnosticsCollector diag,
    bool withErrors
)
{
    if (withErrors)
    {
        // Simulate some failures for demo
        if (instanceName == "sonarr-main" && pipeline == "QS")
        {
            diag.AddError(instanceName, "Invalid quality definition type \"foo\"");
            diag.AddError(instanceName, "Quality \"bar\" not found in guide");
            return (PipelineStatus.Failed, null);
        }

        if (instanceName == "sonarr-main" && pipeline == "CF")
        {
            diag.AddWarning(instanceName, "Invalid trash_id \"abc123\"");
        }

        if (instanceName == "sonarr-main" && pipeline == "QP")
        {
            diag.AddWarning(
                instanceName,
                "Duplicate score for CF \"HDR\" in profile \"HD-1080p\" (using last value: 500)"
            );
        }

        // Add a deprecation warning example (global scope)
        if (instanceName == "radarr-4k" && pipeline == "CF")
        {
            diag.AddDeprecation(
                "'quality_profiles' syntax will be removed in v8.0. Use 'quality_definition' instead. See: https://recyclarr.dev/wiki/upgrade-guide"
            );
        }
    }

    // Return random count for success (including some 3-digit numbers)
    return (PipelineStatus.Succeeded, Random.Shared.Next(1, 250));
}

IRenderable BuildDisplay(List<InstanceState> states, string spinnerFrame)
{
    var table = new Table().Border(TableBorder.None).HideHeaders();

    // Add columns: status icon, instance name, then 4 pipeline columns
    table.AddColumn(new TableColumn("").Width(1));
    table.AddColumn(new TableColumn("Instance").PadRight(2));
    table.AddColumn(new TableColumn("CF").RightAligned());
    table.AddColumn(new TableColumn("QP").RightAligned());
    table.AddColumn(new TableColumn("QS").RightAligned());
    table.AddColumn(new TableColumn("MN").RightAligned());

    // Header row with column names (two-line headers)
    table.AddRow(
        new Markup(""),
        new Markup(""),
        new Markup("[blue]Custom[/]"),
        new Markup("[blue]Quality[/]"),
        new Markup("[blue]Quality[/]"),
        new Markup("[blue]Media[/]")
    );
    table.AddRow(
        new Markup(""),
        new Markup(""),
        new Markup("[blue]Formats[/]"),
        new Markup("[blue]Profiles[/]"),
        new Markup("[blue]Sizes[/]"),
        new Markup("[blue]Naming[/]")
    );

    foreach (var instance in states)
    {
        var isActive = instance.Status == InstanceStatus.Running;
        var boldStyle = isActive ? " bold" : "";

        var statusIcon = instance.Status switch
        {
            InstanceStatus.Pending => $"[grey{boldStyle}]{spinnerFrame}[/]",
            InstanceStatus.Running => $"[blue{boldStyle}]{spinnerFrame}[/]",
            InstanceStatus.Succeeded => $"[green{boldStyle}]✓[/]",
            InstanceStatus.Failed => $"[red{boldStyle}]✗[/]",
            _ => " ",
        };

        var cfStatus = FormatPipelineStatus(
            instance.PipelineResults.GetValueOrDefault("CF"),
            spinnerFrame,
            3,
            isActive
        );
        var qpStatus = FormatPipelineStatus(
            instance.PipelineResults.GetValueOrDefault("QP"),
            spinnerFrame,
            3,
            isActive
        );
        var qsStatus = FormatPipelineStatus(
            instance.PipelineResults.GetValueOrDefault("QS"),
            spinnerFrame,
            3,
            isActive
        );
        var mnStatus = FormatPipelineStatus(
            instance.PipelineResults.GetValueOrDefault("MN"),
            spinnerFrame,
            3,
            isActive
        );

        var nameStyle = isActive ? "white bold" : "white";
        table.AddRow(
            new Markup(statusIcon),
            new Markup($"[{nameStyle}]{instance.Name}[/]"),
            new Markup(cfStatus),
            new Markup(qpStatus),
            new Markup(qsStatus),
            new Markup(mnStatus)
        );
    }

    return table;
}

string FormatPipelineStatus(PipelineResult? result, string spinnerFrame, int width, bool isActive)
{
    string value;
    string color;

    if (result is null)
    {
        value = spinnerFrame;
        color = "grey";
    }
    else
    {
        (value, color) = result.Status switch
        {
            PipelineStatus.Pending => (spinnerFrame, "grey"),
            PipelineStatus.Running => (spinnerFrame, "yellow"),
            PipelineStatus.Succeeded => (
                result.Count?.ToString(CultureInfo.InvariantCulture) ?? "ok",
                "green"
            ),
            PipelineStatus.Failed => ("-", "red"),
            PipelineStatus.Skipped => ("--", "grey"),
            _ => ("?", "white"),
        };
    }

    var boldStyle = isActive ? " bold" : "";
    return $"[{color}{boldStyle}]{value.PadLeft(width)}[/]";
}

void RenderDiagnosticReport(DiagnosticsCollector collector)
{
    if (collector.Errors.Count == 0 && collector.Warnings.Count == 0)
    {
        return;
    }

    // Assign consistent colors to instances
    var instanceColors = new[] { "cyan", "magenta", "blue", "green", "yellow" };
    var allInstances = collector
        .Errors.Concat(collector.Warnings)
        .Select(e => e.Instance)
        .Where(i => !string.IsNullOrEmpty(i))
        .Distinct()
        .ToList();
    var colorMap = allInstances
        .Select((inst, idx) => (inst, color: instanceColors[idx % instanceColors.Length]))
        .ToDictionary(x => x.inst!, x => x.color);

    string FormatPrefix(string? instance)
    {
        if (string.IsNullOrEmpty(instance))
            return "[grey][[global]][/]";
        var color = colorMap.GetValueOrDefault(instance, "white");
        return $"[{color}][[{instance.EscapeMarkup()}]][/]";
    }

    string FormatMessage(DiagnosticEntry entry)
    {
        var deprecationTag =
            entry.Type == DiagnosticType.Deprecation ? "[darkorange bold][[DEPRECATED]][/] " : "";
        return $"{deprecationTag}{entry.Message.EscapeMarkup()}";
    }

    var sections = new List<IRenderable>();

    if (collector.Errors.Count > 0)
    {
        var errorLines = new List<IRenderable>
        {
            new Markup("[red]Errors[/]"),
            new Markup("[red]──────[/]"),
        };
        foreach (var error in collector.Errors)
        {
            var prefix = FormatPrefix(error.Instance);
            errorLines.Add(new Markup($"  [red]•[/] {prefix} {FormatMessage(error)}"));
        }
        sections.Add(new Rows(errorLines));
    }

    if (collector.Warnings.Count > 0)
    {
        if (sections.Count > 0)
            sections.Add(new Text(""));

        var warningLines = new List<IRenderable>
        {
            new Markup("[yellow]Warnings[/]"),
            new Markup("[yellow]────────[/]"),
        };
        foreach (var warning in collector.Warnings)
        {
            var prefix = FormatPrefix(warning.Instance);
            warningLines.Add(new Markup($"  [yellow]•[/] {prefix} {FormatMessage(warning)}"));
        }
        sections.Add(new Rows(warningLines));
    }

    var panel = new Panel(new Rows(sections))
        .Header("[bold]Sync Diagnostics[/]")
        .Border(BoxBorder.Rounded)
        .Expand();

    AnsiConsole.Write(panel);
}

// Supporting types

enum InstanceStatus
{
    Pending,
    Running,
    Succeeded,
    Failed,
}

enum PipelineStatus
{
    Pending,
    Running,
    Succeeded,
    Failed,
    Skipped,
}

[Flags]
enum SupportedPipelines
{
    None = 0,
    Cf = 1,
    Qp = 2,
    Qs = 4,
    Mn = 8,
    All = Cf | Qp | Qs | Mn,
    CfAndQp = Cf | Qp,
}

record PipelineResult(PipelineStatus Status, int? Count);

enum DiagnosticType
{
    Error,
    Warning,
    Deprecation,
}

record DiagnosticEntry(DiagnosticType Type, string? Instance, string Message);

class InstanceState(string name, SupportedPipelines supported)
{
    public string Name { get; } = name;
    public InstanceStatus Status { get; set; } = InstanceStatus.Pending;
    public Dictionary<string, PipelineResult> PipelineResults { get; } = new();

    public bool SupportsPipeline(string pipeline) =>
        pipeline switch
        {
            "CF" => supported.HasFlag(SupportedPipelines.Cf),
            "QP" => supported.HasFlag(SupportedPipelines.Qp),
            "QS" => supported.HasFlag(SupportedPipelines.Qs),
            "MN" => supported.HasFlag(SupportedPipelines.Mn),
            _ => false,
        };
}

class DiagnosticsCollector
{
    private readonly List<DiagnosticEntry> _entries = [];

    public IReadOnlyList<DiagnosticEntry> Errors =>
        _entries.Where(e => e.Type == DiagnosticType.Error).ToList();

    public IReadOnlyList<DiagnosticEntry> Warnings =>
        _entries
            .Where(e => e.Type is DiagnosticType.Warning or DiagnosticType.Deprecation)
            .ToList();

    public IReadOnlyList<DiagnosticEntry> Deprecations =>
        _entries.Where(e => e.Type == DiagnosticType.Deprecation).ToList();

    // Global scope
    public void AddError(string message) =>
        _entries.Add(new DiagnosticEntry(DiagnosticType.Error, null, message));

    public void AddWarning(string message) =>
        _entries.Add(new DiagnosticEntry(DiagnosticType.Warning, null, message));

    public void AddDeprecation(string message) =>
        _entries.Add(new DiagnosticEntry(DiagnosticType.Deprecation, null, message));

    // Instance scope
    public void AddError(string instance, string message) =>
        _entries.Add(new DiagnosticEntry(DiagnosticType.Error, instance, message));

    public void AddWarning(string instance, string message) =>
        _entries.Add(new DiagnosticEntry(DiagnosticType.Warning, instance, message));

    public void AddDeprecation(string instance, string message) =>
        _entries.Add(new DiagnosticEntry(DiagnosticType.Deprecation, instance, message));
}
