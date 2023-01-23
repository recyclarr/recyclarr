using Spectre.Console;

namespace Recyclarr.TrashLib.Pipelines.QualityProfile.PipelinePhases;

public class QualityProfilePreviewPhase
{
    private readonly IAnsiConsole _console;

    public QualityProfilePreviewPhase(IAnsiConsole console)
    {
        _console = console;
    }

    public void Execute(QualityProfileTransactionData transactions)
    {
        var profileScoreUpdates = transactions.UpdatedProfiles
            .ToDictionary(x => x.UpdatedProfile.Name, x => x.UpdatedScores);

        var tree = new Tree("Quality Profiles Scores [red](Preview)[/]");

        foreach (var (profileName, updatedScores) in profileScoreUpdates)
        {
            var table = new Table()
                .AddColumn("[bold]Custom Format[/]")
                .AddColumn("[bold]Current[/]")
                .AddColumn("[bold]New[/]")
                .AddColumn("[bold]Reason[/]");

            foreach (var updatedScore in updatedScores)
            {
                table.AddRow(
                    updatedScore.CustomFormatName,
                    updatedScore.OldScore.ToString(),
                    updatedScore.NewScore.ToString(),
                    updatedScore.Reason.ToString());
            }

            tree.AddNode($"[yellow]{profileName}[/]")
                .AddNode(table);
        }

        _console.WriteLine();
        _console.Write(tree);
        _console.WriteLine();

        if (transactions.InvalidProfileNames.Any())
        {
            _console.MarkupLine("The following quality profiles were [red]not found[/]:");
            foreach (var name in transactions.InvalidProfileNames)
            {
                _console.MarkupLine($"[red]x[/] {name}");
            }

            _console.WriteLine();
        }
    }
}
