using System.Globalization;
using Recyclarr.Client.V1;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.Preview;

internal static class QualityProfilePreviewRenderer
{
    public static void Render(IAnsiConsole console, QualityProfilesResultResponse result)
    {
        if (result.Profiles.Count == 0)
        {
            console.MarkupLine("[dim]No changes[/]");
            return;
        }

        foreach (var profile in result.Profiles)
        {
            RenderProfileTree(console, profile);
        }
    }

    private static void RenderProfileTree(
        IAnsiConsole console,
        QualityProfileChangeResponse profile
    )
    {
        var profileTree = new Tree(
            Markup.FromInterpolated(
                CultureInfo.InvariantCulture,
                $"[yellow]{profile.Name}[/] (Change Reason: [green]{profile.ChangeReason}[/])"
            )
        );

        profileTree.AddNode(
            new Rows(new Markup("[b]Profile Updates[/]"), SetupProfileTable(profile))
        );

        if (profile.Qualities is { } qualities)
        {
            profileTree.AddNode(SetupQualityItemTable(qualities));
        }

        profileTree.AddNode(new Rows(new Markup("[b]Score Updates[/]"), SetupScoreTable(profile)));

        console.Write(profileTree);
        console.WriteLine();
    }

    private static Table SetupProfileTable(QualityProfileChangeResponse profile)
    {
        var table = new Table()
            .AddColumn("[bold]Profile Field[/]")
            .AddColumn("[bold]Current[/]")
            .AddColumn("[bold]New[/]");

        var oldProfile = profile.Current;
        var newProfile = profile.Desired;

        table.AddRow("Name", Markup.Escape(oldProfile.Name), Markup.Escape(newProfile.Name));
        table.AddRow(
            "Upgrades Allowed?",
            YesNo(oldProfile.UpgradeAllowed),
            YesNo(newProfile.UpgradeAllowed)
        );
        table.AddRow(
            "Minimum Format Score",
            Null(oldProfile.MinFormatScore),
            Null(newProfile.MinFormatScore)
        );
        table.AddRow(
            "Minimum Format Upgrade Score",
            Null(oldProfile.MinUpgradeFormatScore),
            Null(newProfile.MinUpgradeFormatScore)
        );

        if (newProfile.UpgradeAllowed is true)
        {
            table.AddRow(
                "Upgrade Until Quality",
                Null(oldProfile.UpgradeUntilQuality),
                Null(newProfile.UpgradeUntilQuality)
            );

            table.AddRow(
                "Upgrade Until Score",
                Null(oldProfile.UpgradeUntilScore),
                Null(newProfile.UpgradeUntilScore)
            );
        }

        return table;

        static string YesNo(bool? val) => val is true ? "Yes" : "No";

        static string Null<T>(T? val) =>
            val is null ? "<unset>" : Markup.Escape(val.ToString() ?? "<invalid>");
    }

    private static Rows SetupQualityItemTable(QualityProfileQualitiesResponse qualities)
    {
        var columns = new Columns(
            MakePanel(qualities.Current, "Current"),
            MakePanel(qualities.Desired, "New")
        );

        columns.Collapse();

        return new Rows(
            Markup.FromInterpolated(
                CultureInfo.InvariantCulture,
                $"[b]Quality Updates (Sort Mode: [green]{qualities.SortMode}[/])[/]"
            ),
            columns
        );

        static IRenderable BuildItemName(QualityItemResponse item)
        {
            var allowedChar = item.Allowed ? ":check_mark:" : ":cross_mark:";
            var name = string.IsNullOrEmpty(item.Name) ? "NO NAME!" : item.Name;
            return Markup.FromInterpolated(CultureInfo.InvariantCulture, $"{allowedChar} {name}");
        }

        static IRenderable BuildGroupTree(QualityItemResponse item)
        {
            var tree = new Tree(BuildItemName(item));
            foreach (var child in item.Items ?? [])
            {
                tree.AddNode(BuildItemName(child));
            }

            return tree;
        }

        // Only groups carry nested items; a plain quality renders as a single line.
        static IRenderable MakeNode(QualityItemResponse item) =>
            item.Items is { Count: > 0 } ? BuildGroupTree(item) : BuildItemName(item);

        static IRenderable MakePanel(IEnumerable<QualityItemResponse> items, string header)
        {
            var headerMarkup = Markup.FromInterpolated(
                CultureInfo.InvariantCulture,
                $"[bold][underline]{header}[/][/]"
            );
            IEnumerable<IRenderable> rowItems = [headerMarkup, .. items.Select(MakeNode)];
            var panel = new Panel(new Rows(rowItems)).NoBorder();
            panel.Width = 23;
            return panel;
        }
    }

    private static IRenderable SetupScoreTable(QualityProfileChangeResponse profile)
    {
        if (profile.ScoreChanges.Count == 0)
        {
            return new Markup("[hotpink]No score changes[/]");
        }

        var table = new Table()
            .AddColumn("[bold]Custom Format[/]")
            .AddColumn("[bold]Current[/]")
            .AddColumn("[bold]New[/]")
            .AddColumn("[bold]Reason[/]");

        foreach (var score in profile.ScoreChanges)
        {
            table.AddRow(
                Markup.Escape(score.Name),
                score.CurrentScore.ToString(CultureInfo.InvariantCulture),
                score.NewScore.ToString(CultureInfo.InvariantCulture),
                score.Reason
            );
        }

        return table;
    }
}
