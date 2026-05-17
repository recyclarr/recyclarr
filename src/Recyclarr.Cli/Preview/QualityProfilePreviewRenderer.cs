using System.Globalization;
using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Servarr.QualityProfile;
using Recyclarr.Sync;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.Preview;

internal static class QualityProfilePreviewRenderer
{
    public static void Render(IAnsiConsole console, QualityProfileSyncResult result)
    {
        var transactions = result.Transactions;
        var totalChanges = transactions.NewProfiles.Count + transactions.UpdatedProfiles.Count;

        if (totalChanges == 0)
        {
            console.MarkupLine("[dim]No changes[/]");
            return;
        }

        foreach (var profile in transactions.NewProfiles)
        {
            RenderProfileTree(console, profile, "New");
        }

        foreach (var profileWithStats in transactions.UpdatedProfiles)
        {
            RenderProfileTree(console, profileWithStats.Profile, "Changed");
        }
    }

    private static void RenderProfileTree(
        IAnsiConsole console,
        UpdatedQualityProfile profile,
        string changeReason
    )
    {
        var profileTree = new Tree(
            Markup.FromInterpolated(
                CultureInfo.InvariantCulture,
                $"[yellow]{profile.ProfileName}[/] (Change Reason: [green]{changeReason}[/])"
            )
        );

        profileTree.AddNode(
            new Rows(new Markup("[b]Profile Updates[/]"), SetupProfileTable(profile))
        );

        if (profile.HasQualityOverrides)
        {
            profileTree.AddNode(SetupQualityItemTable(profile));
        }

        profileTree.AddNode(new Rows(new Markup("[b]Score Updates[/]"), SetupScoreTable(profile)));

        console.Write(profileTree);
        console.WriteLine();
    }

    private static Table SetupProfileTable(UpdatedQualityProfile profile)
    {
        var table = new Table()
            .AddColumn("[bold]Profile Field[/]")
            .AddColumn("[bold]Current[/]")
            .AddColumn("[bold]New[/]");

        var oldProfile = profile.Profile;
        var newProfile = profile.BuildMergedProfile();

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
                Null(oldProfile.Items.FindCutoff(oldProfile.Cutoff)),
                Null(newProfile.Items.FindCutoff(newProfile.Cutoff))
            );

            table.AddRow(
                "Upgrade Until Score",
                Null(oldProfile.CutoffFormatScore),
                Null(newProfile.CutoffFormatScore)
            );
        }

        return table;

        static string YesNo(bool? val) => val is true ? "Yes" : "No";

        static string Null<T>(T? val) =>
            val is null ? "<unset>" : Markup.Escape(val.ToString() ?? "<invalid>");
    }

    private static Rows SetupQualityItemTable(UpdatedQualityProfile profile)
    {
        var columns = new Columns(
            MakePanel(profile.Profile.Items, "Current"),
            MakePanel(profile.UpdatedQualities.Items, "New")
        );

        columns.Collapse();

        var sortMode = profile.QualitySort;
        return new Rows(
            Markup.FromInterpolated(
                CultureInfo.InvariantCulture,
                $"[b]Quality Updates (Sort Mode: [green]{sortMode}[/])[/]"
            ),
            columns
        );

        static IRenderable BuildItemName(QualityProfileItem item)
        {
            var allowedChar = item.Allowed is true ? ":check_mark:" : ":cross_mark:";
            var name = item.Quality?.Name ?? item.Name ?? "NO NAME!";
            return Markup.FromInterpolated(CultureInfo.InvariantCulture, $"{allowedChar} {name}");
        }

        static IRenderable BuildGroupTree(QualityProfileItem item)
        {
            var tree = new Tree(BuildItemName(item));
            foreach (var child in item.Items)
            {
                tree.AddNode(BuildItemName(child));
            }

            return tree;
        }

        static IRenderable MakeNode(QualityProfileItem item) =>
            item.Quality is not null ? BuildItemName(item) : BuildGroupTree(item);

        static IRenderable MakePanel(IEnumerable<QualityProfileItem> items, string header)
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

    private static IRenderable SetupScoreTable(UpdatedQualityProfile profile)
    {
        // Only show scores that changed value (not just reason)
        var updatedScores = profile
            .UpdatedScores.Where(x =>
                x.Reason != FormatScoreUpdateReason.NoChange && x.FormatItem.Score != x.NewScore
            )
            .ToList();

        if (updatedScores.Count == 0)
        {
            return new Markup("[hotpink]No score changes[/]");
        }

        var table = new Table()
            .AddColumn("[bold]Custom Format[/]")
            .AddColumn("[bold]Current[/]")
            .AddColumn("[bold]New[/]")
            .AddColumn("[bold]Reason[/]");

        foreach (var score in updatedScores)
        {
            table.AddRow(
                Markup.Escape(score.FormatItem.Name),
                score.FormatItem.Score.ToString(CultureInfo.InvariantCulture),
                score.NewScore.ToString(CultureInfo.InvariantCulture),
                score.Reason.ToString()
            );
        }

        return table;
    }
}
