using System.Globalization;
using Recyclarr.ServarrApi.QualityProfile;
using Recyclarr.Sync;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

internal class QualityProfilePreviewPhase(IAnsiConsole console, ISyncContextSource contextSource)
    : PreviewPipelinePhase<QualityProfilePipelineContext>(console, contextSource)
{
    protected override void RenderPreview(QualityProfilePipelineContext context)
    {
        RenderTitle(context);

        if (context.TransactionOutput.ChangedProfiles.Count == 0)
        {
            Console.MarkupLine("[dim]No changes[/]");
            return;
        }

        foreach (var profile in context.TransactionOutput.ChangedProfiles.Select(x => x.Profile))
        {
            var profileTree = new Tree(
                Markup.FromInterpolated(
                    CultureInfo.InvariantCulture,
                    $"[yellow]{profile.ProfileName}[/] (Change Reason: [green]{profile.UpdateReason}[/])"
                )
            );

            profileTree.AddNode(
                new Rows(new Markup("[b]Profile Updates[/]"), SetupProfileTable(profile))
            );

            if (profile.ProfileConfig.Config.Qualities.Count != 0)
            {
                profileTree.AddNode(SetupQualityItemTable(profile));
            }

            profileTree.AddNode(
                new Rows(new Markup("[b]Score Updates[/]"), SetupScoreTable(profile))
            );

            Console.Write(profileTree);
            Console.WriteLine();
        }
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
        table.AddRow(
            "Upgrades Allowed?",
            YesNo(oldDto.UpgradeAllowed),
            YesNo(newDto.UpgradeAllowed)
        );
        table.AddRow(
            "Minimum Format Score",
            Null(oldDto.MinFormatScore),
            Null(newDto.MinFormatScore)
        );
        table.AddRow(
            "Minimum Format Upgrade Score",
            Null(oldDto.MinUpgradeFormatScore),
            Null(newDto.MinUpgradeFormatScore)
        );

        // ReSharper disable once InvertIf
        if (newDto.UpgradeAllowed is true)
        {
            table.AddRow(
                "Upgrade Until Quality",
                Null(oldDto.Items.FindCutoff(oldDto.Cutoff)),
                Null(newDto.Items.FindCutoff(newDto.Cutoff))
            );

            table.AddRow(
                "Upgrade Until Score",
                Null(oldDto.CutoffFormatScore),
                Null(newDto.CutoffFormatScore)
            );
        }

        return table;

        static string YesNo(bool? val) => val is true ? "Yes" : "No";
        static string Null<T>(T? val) => val is null ? "<unset>" : val.ToString() ?? "<invalid>";
    }

    private static Rows SetupQualityItemTable(UpdatedQualityProfile profile)
    {
        static IRenderable BuildName(ProfileItemDto item)
        {
            var allowedChar = item.Allowed is true ? ":check_mark:" : ":cross_mark:";
            var name = item.Quality?.Name ?? item.Name ?? "NO NAME!";
            return Markup.FromInterpolated(CultureInfo.InvariantCulture, $"{allowedChar} {name}");
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
            MakeTree(profile.ProfileDto.Items, "Current"),
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

    private static IRenderable SetupScoreTable(UpdatedQualityProfile profile)
    {
        var updatedScores = profile
            .UpdatedScores.Where(x =>
                x.Reason != FormatScoreUpdateReason.NoChange && x.Dto.Score != x.NewScore
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
                score.Dto.Name,
                score.Dto.Score.ToString(CultureInfo.InvariantCulture),
                score.NewScore.ToString(CultureInfo.InvariantCulture),
                score.Reason.ToString()
            );
        }

        return table;
    }
}
