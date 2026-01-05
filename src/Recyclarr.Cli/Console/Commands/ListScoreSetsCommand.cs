using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Pipelines.CustomFormat;
using Recyclarr.Cli.Processors;
using Recyclarr.TrashGuide;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

#pragma warning disable CS8765
[UsedImplicitly]
[Description("List score sets available for custom formats.")]
internal class ListScoreSetsCommand(
    ILogger log,
    IAnsiConsole console,
    CategorizedCustomFormatProvider provider,
    ProviderProgressHandler providerProgressHandler
) : AsyncCommand<ListScoreSetsCommand.CliSettings>
{
    [UsedImplicitly]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    internal class CliSettings : BaseCommandSettings
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

        console.WriteLine(
            "\nThe following score sets are available. Use these with the `score_set` property in any "
                + "quality profile defined under the top-level `quality_profiles` list."
        );

        console.WriteLine();

        var customFormats = provider.Get(settings.Service);

        var scoreSets = customFormats
            .SelectMany(x => x.Resource.TrashScores.Keys)
            .Distinct(StringComparer.InvariantCultureIgnoreCase)
            .Order(StringComparer.InvariantCultureIgnoreCase)
            .ToList();

        log.Debug("Found {Count} score sets for {Service}", scoreSets.Count, settings.Service);
        log.Information("Score sets: {@ScoreSets}", scoreSets);

        foreach (var set in scoreSets)
        {
            console.WriteLine($"  - {set}");
        }

        return (int)ExitStatus.Succeeded;
    }
}
