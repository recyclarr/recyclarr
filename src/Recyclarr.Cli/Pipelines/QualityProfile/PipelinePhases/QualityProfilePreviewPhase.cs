using Recyclarr.ServarrApi.QualityProfile;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

public class QualityProfilePreviewPhase(IAnsiConsole console)
{
    public void Execute(QualityProfileTransactionData transactions)
    {
        var tree = new Tree("Quality Profile Changes [red](Preview)[/]");

        foreach (var profile in transactions.UpdatedProfiles)
        {
            var profileTree = new Tree(Markup.FromInterpolated(
                $"[yellow]{profile.ProfileName}[/] (Change Reason: [green]{profile.UpdateReason}[/])"));

            profileTree.AddNode(new Rows(
                new Markup("[b]Profile Updates[/]"),
                SetupProfileTable(profile)));

            if (profile.ProfileConfig.Profile.Qualities.Any())
            {
                profileTree.AddNode(SetupQualityItemTable(profile));
            }

            profileTree.AddNode(new Rows(
                new Markup("[b]Score Updates[/]"),
                SetupScoreTable(profile)));

            tree.AddNode(profileTree);
        }

        console.WriteLine();
        console.Write(tree);
        console.WriteLine();
    }

    private static Table SetupProfileTable(UpdatedQualityProfile profile)
    {
        var table = new Table()
            .AddColumn("[bold]Profile Field[/]")
            .AddColumn("[bold]Current[/]")
            .AddColumn("[bold]New[/]");

        var oldDto = profile.ProfileDto;
        var newDto = profile.BuildUpdatedDto();

        table.AddRow("Name", oldDto.Name, newDto.Name);
        table.AddRow("Upgrades Allowed?", YesNo(oldDto.UpgradeAllowed), YesNo(newDto.UpgradeAllowed));
        table.AddRow("Minimum Format Score", Null(oldDto.MinFormatScore), Null(newDto.MinFormatScore));

        // ReSharper disable once InvertIf
        if (newDto.UpgradeAllowed is true)
        {
            table.AddRow("Upgrade Until Quality",
                Null(oldDto.Items.FindCutoff(oldDto.Cutoff)),
                Null(newDto.Items.FindCutoff(newDto.Cutoff)));

            table.AddRow("Upgrade Until Score",
                Null(oldDto.CutoffFormatScore),
                Null(newDto.CutoffFormatScore));
        }

        return table;

        static string YesNo(bool? val) => val is true ? "Yes" : "No";
        static string Null<T>(T? val) => val is null ? "<unset>" : val.ToString() ?? "<invalid>";
    }

    private static IRenderable SetupQualityItemTable(UpdatedQualityProfile profile)
    {
        static IRenderable BuildName(ProfileItemDto item)
        {
            var allowedChar = item.Allowed is true ? ":check_mark:" : ":cross_mark:";
            var name = item.Quality?.Name ?? item.Name ?? "NO NAME!";
            return Markup.FromInterpolated($"{allowedChar} {name}");
        }

        static IRenderable BuildTree(ProfileItemDto item)
        {
            var tree = new Tree(BuildName(item));
            foreach (var childItem in item.Items)
            {
                tree.AddNode(BuildName(childItem));
            }

            return tree;
        }

        static IRenderable MakeNode(ProfileItemDto item)
        {
            return item.Quality is not null ? BuildName(item) : BuildTree(item);
        }

        static IRenderable MakeTree(IEnumerable<ProfileItemDto> items, string header)
        {
            var headerMarkup = Markup.FromInterpolated($"[bold][underline]{header}[/][/]");
            var rows = new Rows(new[] {headerMarkup}.Concat(items.Select(MakeNode)));
            var panel = new Panel(rows).NoBorder();
            panel.Width = 23;
            return panel;
        }

        var table = new Columns(
            MakeTree(profile.ProfileDto.Items, "Current"),
            MakeTree(profile.UpdatedQualities.Items, "New")
        );

        table.Collapse();

        var sortMode = profile.ProfileConfig.Profile.QualitySort;
        return new Rows(
            Markup.FromInterpolated($"[b]Quality Updates (Sort Mode: [green]{sortMode}[/])[/]"),
            table);
    }

    private static IRenderable SetupScoreTable(UpdatedQualityProfile profile)
    {
        var updatedScores = profile.UpdatedScores
            .Where(x => x.Reason != FormatScoreUpdateReason.NoChange && x.Dto.Score != x.NewScore)
            .ToList();

        if (!updatedScores.Any())
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
                score.Dto.Name,
                score.Dto.Score.ToString(),
                score.NewScore.ToString(),
                score.Reason.ToString());
        }

        return table;
    }
}
