using System.Globalization;
using Recyclarr.Servarr.QualityProfile;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

internal class QualityProfilePreviewPhase(IAnsiConsole console)
    : PreviewPipelinePhase<QualityProfilePipelineContext>(console)
{
    protected override void RenderPreview(QualityProfilePipelineContext context)
    {
        RenderTitle(context);

        var transactions = context.TransactionOutput;
        var totalChanges = transactions.NewProfiles.Count + transactions.UpdatedProfiles.Count;

        if (totalChanges == 0)
        {
            Console.MarkupLine("[dim]No changes[/]");
            return;
        }

        // Render new profiles
        foreach (var profile in transactions.NewProfiles)
        {
            RenderProfileTree(profile, "New");
        }

        // Render updated profiles
        foreach (var profileWithStats in transactions.UpdatedProfiles)
        {
            RenderProfileTree(profileWithStats.Profile, "Changed");
        }
    }

    private void RenderProfileTree(UpdatedQualityProfile profile, string changeReason)
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

        if (profile.ProfileConfig.Config.Qualities.Count != 0)
        {
            profileTree.AddNode(SetupQualityItemTable(profile));
        }

        if (profile.AssignedSizes.Count != 0)
        {
            profileTree.AddNode(
                new Rows(new Markup("[b]Quality Size Updates[/]"), SetupSizeTable(profile))
            );
        }

        profileTree.AddNode(new Rows(new Markup("[b]Score Updates[/]"), SetupScoreTable(profile)));

        Console.Write(profileTree);
        Console.WriteLine();
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

        // ReSharper disable once InvertIf
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
        static IRenderable BuildName(QualityProfileItem item)
        {
            var allowedChar = item.Allowed is true ? ":check_mark:" : ":cross_mark:";
            var name = item.Quality?.Name ?? item.Name ?? "NO NAME!";
            return Markup.FromInterpolated(CultureInfo.InvariantCulture, $"{allowedChar} {name}");
        }

        static IRenderable BuildTree(QualityProfileItem item)
        {
            var tree = new Tree(BuildName(item));
            foreach (var childItem in item.Items)
            {
                tree.AddNode(BuildName(childItem));
            }

            return tree;
        }

        static IRenderable MakeNode(QualityProfileItem item)
        {
            return item.Quality is not null ? BuildName(item) : BuildTree(item);
        }

        static IRenderable MakeTree(IEnumerable<QualityProfileItem> items, string header)
        {
            var headerMarkup = Markup.FromInterpolated(
                CultureInfo.InvariantCulture,
                $"[bold][underline]{header}[/][/]"
            );
            var rows = new Rows(new[] { headerMarkup }.Concat(items.Select(MakeNode)));
            var panel = new Panel(rows).NoBorder();
            panel.Width = 23;
            return panel;
        }

        var table = new Columns(
            MakeTree(profile.Profile.Items, "Current"),
            MakeTree(profile.UpdatedQualities.Items, "New")
        );

        table.Collapse();

        var sortMode = profile.ProfileConfig.Config.QualitySort;
        return new Rows(
            Markup.FromInterpolated(
                CultureInfo.InvariantCulture,
                $"[b]Quality Updates (Sort Mode: [green]{sortMode}[/])[/]"
            ),
            table
        );
    }

    private static IRenderable SetupSizeTable(UpdatedQualityProfile profile)
    {
        static IEnumerable<QualityProfileItem> Flatten(IEnumerable<QualityProfileItem> items) =>
            items.SelectMany(x => x.Quality is not null ? [x] : Flatten(x.Items));

        var currentSizes = Flatten(profile.Profile.Items)
            .Where(x => x.Quality?.Name is not null)
            .ToDictionary(x => x.Quality!.Name!, x => x.Size); // non-null: filtered above

        var changes = Flatten(profile.BuildMergedProfile().Items)
            .Select(x => (Item: x, Current: currentSizes.GetValueOrDefault(x.Quality?.Name ?? "")))
            .Where(x => x.Item.Size != x.Current)
            .ToList();

        if (changes.Count == 0)
        {
            return new Markup("[hotpink]No size changes[/]");
        }

        var table = new Table()
            .AddColumn("[bold]Quality[/]")
            .AddColumn("[bold]Current (Min / Preferred / Max)[/]")
            .AddColumn("[bold]New (Min / Preferred / Max)[/]");

        foreach (var (item, current) in changes)
        {
            table.AddRow(
                Markup.Escape(item.Quality?.Name ?? ""),
                FormatSize(current),
                FormatSize(item.Size)
            );
        }

        return table;

        static string FormatSize(QualityItemSize? size) =>
            size is null
                ? "<unset>"
                : string.Create(
                    CultureInfo.InvariantCulture,
                    $"{size.Min} / {Limit(size.Preferred)} / {Limit(size.Max)}"
                );

        static string Limit(decimal? value) =>
            value?.ToString(CultureInfo.InvariantCulture) ?? "Unlimited";
    }

    private static IRenderable SetupScoreTable(UpdatedQualityProfile profile)
    {
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
