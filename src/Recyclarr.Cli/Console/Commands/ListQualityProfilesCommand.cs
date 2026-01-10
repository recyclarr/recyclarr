using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Processors;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.TrashGuide;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("List quality profiles in the guide for a particular service.")]
internal class ListQualityProfilesCommand(
    ILogger log,
    IAnsiConsole console,
    QualityProfileResourceQuery guide,
    ProviderProgressHandler providerProgressHandler
) : AsyncCommand<ListQualityProfilesCommand.CliSettings>
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

        var profiles = guide.Get(settings.Service).OrderBy(p => p.Name).ToList();

        log.Debug("Found {Count} quality profiles for {Service}", profiles.Count, settings.Service);

        if (settings.Raw)
        {
            OutputRaw(profiles);
        }
        else
        {
            OutputTable(profiles, settings.Service);
        }

        return (int)ExitStatus.Succeeded;
    }

    private void OutputRaw(IReadOnlyCollection<QualityProfileResource> profiles)
    {
        foreach (var profile in profiles)
        {
            console.WriteLine($"{profile.TrashId}\t{profile.Name}\t{profile.TrashUrl}");
        }
    }

    private void OutputTable(
        IReadOnlyCollection<QualityProfileResource> profiles,
        SupportedServices service
    )
    {
        console.WriteLine();
        console.MarkupLine($"[orange3]Quality Profiles in the TRaSH Guides ({service})[/]");
        console.MarkupLine("[dim]Tip: Click profile names to open the guide page.[/]");
        console.WriteLine();

        var table = new Table().RoundedBorder().ShowRowSeparators();

        table.AddColumn("Profile");
        table.AddColumn("Trash ID");

        foreach (var profile in profiles)
        {
            AddProfileRow(table, profile);
        }

        console.Write(table);
        console.WriteLine();
        console.WriteLine(
            "Copy the Trash ID to use with `trash_ids:` under `quality_profiles:` in your config."
        );
    }

    private static void AddProfileRow(Table table, QualityProfileResource profile)
    {
        var nameMarkup = string.IsNullOrEmpty(profile.TrashUrl)
            ? Markup.Escape(profile.Name)
            : $"[link={profile.TrashUrl}]{Markup.Escape(profile.Name)}[/]";

        var rows = new List<IRenderable> { new Markup($"[bold blue]{nameMarkup}[/]") };

        foreach (var quality in profile.Items.Where(q => q.Allowed))
        {
            rows.Add(FormatQualityRow(quality));
        }

        table.AddRow(new Rows(rows), new Markup($"[dim]{profile.TrashId}[/]"));
    }

    private static Grid FormatQualityRow(QualityProfileQualityItem quality)
    {
        var name = Markup.Escape(quality.Name);
        var qualityText =
            quality.Items.Count == 0
                ? name
                : $"{name} [dim][[{string.Join(", ", quality.Items.Select(Markup.Escape))}]][/]";

        var grid = new Grid();
        grid.AddColumn(new GridColumn().Width(2).NoWrap().PadLeft(2).PadRight(1));
        grid.AddColumn(new GridColumn().PadLeft(0));
        grid.AddRow(new Markup("[green]:check_mark:[/]"), new Markup(qualityText));

        return grid;
    }
}
