using Recyclarr.TrashLib.Pipelines.ReleaseProfile.Api.Objects;
using Recyclarr.TrashLib.Pipelines.ReleaseProfile.Models;
using Spectre.Console;

namespace Recyclarr.TrashLib.Pipelines.ReleaseProfile.PipelinePhases;

public class ReleaseProfilePreviewPhase
{
    private readonly IAnsiConsole _console;

    public ReleaseProfilePreviewPhase(IAnsiConsole console)
    {
        _console = console;
    }

    public void Execute(ReleaseProfileTransactionData profiles)
    {
        var tree = new Tree("Release Profiles [red](Preview)[/]");

        PrintCategoryOfChanges("Created Profiles", tree, profiles.CreatedProfiles);
        PrintCategoryOfChanges("Updated Profiles", tree, profiles.UpdatedProfiles);

        _console.WriteLine();
        _console.Write(tree);
    }

    private void PrintCategoryOfChanges(string nodeTitle, Tree tree, IEnumerable<SonarrReleaseProfile> profiles)
    {
        var treeNode = tree.AddNode($"[green]{nodeTitle}[/]");
        foreach (var profile in profiles)
        {
            PrintTermsAndScores(treeNode, profile);
        }
    }

    private void PrintTermsAndScores(TreeNode tree, SonarrReleaseProfile profile)
    {
        var rpNode = tree.AddNode($"[yellow]{Markup.Escape(profile.Name)}[/]");

        var incPreferred = profile.IncludePreferredWhenRenaming ? "[green]YES[/]" : "[red]NO[/]";
        rpNode.AddNode($"Include Preferred when Renaming? {incPreferred}");

        PrintTerms(rpNode, "Must Contain", profile.Required);
        PrintTerms(rpNode, "Must Not Contain", profile.Ignored);
        PrintPreferredTerms(rpNode, "Preferred", profile.Preferred);

        _console.WriteLine("");
    }

    private static void PrintTerms(TreeNode tree, string title, IReadOnlyCollection<string> terms)
    {
        if (terms.Count == 0)
        {
            return;
        }

        var table = new Table()
            .AddColumn("[bold]Term[/]");

        foreach (var term in terms)
        {
            table.AddRow(Markup.Escape(term));
        }

        tree.AddNode(title)
            .AddNode(table);
    }

    private static void PrintPreferredTerms(
        TreeNode tree,
        string title,
        IReadOnlyCollection<SonarrPreferredTerm> preferredTerms)
    {
        if (preferredTerms.Count <= 0)
        {
            return;
        }

        var table = new Table()
            .AddColumn("[bold]Score[/]")
            .AddColumn("[bold]Term[/]");

        foreach (var term in preferredTerms)
        {
            table.AddRow(term.Score.ToString(), Markup.Escape(term.Term));
        }

        tree.AddNode(title)
            .AddNode(table);
    }
}
