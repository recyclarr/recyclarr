using Recyclarr.Cli.Pipelines.QualityProfile.Api;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

public class QualityProfilePreviewPhase
{
    private readonly IAnsiConsole _console;

    public QualityProfilePreviewPhase(IAnsiConsole console)
    {
        _console = console;
    }

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

        _console.WriteLine();
        _console.Write(tree);
        _console.WriteLine();
    }

    private static Table SetupProfileTable(UpdatedQualityProfile profile)
    {
        var table = new Table()
            .AddColumn("[bold]Profile Field[/]")
            .AddColumn("[bold]Current[/]")
            .AddColumn("[bold]New[/]");

        static string YesNo(bool? val) => val is true ? "Yes" : "No";
        static string Null<T>(T? val) => val is null ? "<unset>" : val.ToString() ?? "<invalid>";

        var dto = profile.ProfileDto;
        var config = profile.ProfileConfig.Profile;

        table.AddRow("Name", dto.Name, config.Name);
        table.AddRow("Upgrades Allowed?", YesNo(dto.UpgradeAllowed), YesNo(config.UpgradeAllowed));

        if (config.UpgradeUntilQuality is not null)
        {
            table.AddRow("Upgrade Until Quality",
                Null(dto.Items.FindGroupById(dto.Cutoff)?.Name),
                Null(config.UpgradeUntilQuality));
        }

        if (config.MinFormatScore is not null)
        {
            table.AddRow("Minimum Format Score",
                Null(dto.MinFormatScore),
                Null(config.MinFormatScore));
        }

        if (config.UpgradeUntilScore is not null)
        {
            table.AddRow("Upgrade Until Score",
                Null(dto.CutoffFormatScore),
                Null(config.UpgradeUntilScore));
        }

        return table;
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
