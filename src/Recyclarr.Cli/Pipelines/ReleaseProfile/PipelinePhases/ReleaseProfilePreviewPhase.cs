using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.ServarrApi.ReleaseProfile;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile.PipelinePhases;

public class ReleaseProfilePreviewPhase(IAnsiConsole console) : IPreviewPipelinePhase<ReleaseProfilePipelineContext>
{
    public void Execute(ReleaseProfilePipelineContext context)
    {
        var profiles = context.TransactionOutput;

        var tree = new Tree("Release Profiles [red](Preview)[/]");

        PrintCategoryOfChanges("Created Profiles", tree, profiles.CreatedProfiles);
        PrintCategoryOfChanges("Updated Profiles", tree, profiles.UpdatedProfiles);

        console.WriteLine();
        console.Write(tree);
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

        console.WriteLine("");
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
