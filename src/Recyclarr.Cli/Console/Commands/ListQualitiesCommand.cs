using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Server;
using Recyclarr.Client.V1;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("List quality definitions in the guide for a particular service.")]
internal class ListQualitiesCommand(
    ILogger log,
    IAnsiConsole console,
    ServerConnectionFactory connections
) : AsyncCommand<ListQualitiesCommand.CliSettings>
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
        var response = await connection.Guide.Qualities(settings.Service, ct);
        var qualitySizes = response.ContentOrThrow().Items.ToList();

        log.Debug(
            "Found {Count} quality definition types for {Service}",
            qualitySizes.Count,
            settings.Service
        );

        if (settings.Raw)
        {
            OutputRaw(qualitySizes);
        }
        else
        {
            OutputTable(qualitySizes);
        }

        return (int)ExitStatus.Succeeded;
    }

    private void OutputRaw(IReadOnlyCollection<GuideQualitySummaryResponse> qualitySizes)
    {
        foreach (var q in qualitySizes)
        {
            console.WriteRawLine(q.Type);
        }
    }

    private void OutputTable(IReadOnlyCollection<GuideQualitySummaryResponse> qualitySizes)
    {
        var table = new Table().AddColumn("Quality Type");
        var alternatingColors = new[] { "white", "paleturquoise4" };
        var colorIndex = 0;

        foreach (var q in qualitySizes)
        {
            var color = alternatingColors[colorIndex];
            table.AddRow($"[{color}]{Markup.Escape(q.Type)}[/]");
            colorIndex = 1 - colorIndex;
        }

        console.WriteLine();
        console.MarkupLine("[orange3]Quality Definition Types in the TRaSH Guides[/]");
        console.WriteLine();
        console.Write(table);
        console.WriteLine();
        console.WriteLine(
            "Use these with the `quality_definition:` property in your recyclarr.yml file."
        );
    }
}
