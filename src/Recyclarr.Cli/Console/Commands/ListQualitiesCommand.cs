using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Processors;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.TrashGuide;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("List quality definitions in the guide for a particular service.")]
internal class ListQualitiesCommand(
    ILogger log,
    IAnsiConsole console,
    QualitySizeResourceQuery guide,
    ProviderProgressHandler providerProgressHandler
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

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CliSettings settings,
        CancellationToken ct
    )
    {
        await providerProgressHandler.InitializeProvidersAsync(ct);

        var qualitySizes = guide.Get(settings.Service).ToList();

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

    private void OutputRaw(IReadOnlyCollection<QualitySizeResource> qualitySizes)
    {
        foreach (var q in qualitySizes)
        {
            console.WriteLine(q.Type);
        }
    }

    private void OutputTable(IReadOnlyCollection<QualitySizeResource> qualitySizes)
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
