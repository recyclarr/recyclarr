using Recyclarr.TrashGuide.QualitySize;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

public class QualitySizePreviewPhase
{
    private readonly IAnsiConsole _console;

    public QualitySizePreviewPhase(IAnsiConsole console)
    {
        _console = console;
    }

    public void Execute(QualitySizeData selectedQuality)
    {
        var table = new Table();

        table.Title("Quality Sizes [red](Preview)[/]");
        table.AddColumn("[bold]Quality[/]");
        table.AddColumn("[bold]Min[/]");
        table.AddColumn("[bold]Max[/]");
        table.AddColumn("[bold]Preferred[/]");

        foreach (var q in selectedQuality.Qualities)
        {
            var quality = $"[dodgerblue1]{q.Quality}[/]";
            table.AddRow(quality, q.AnnotatedMin, q.AnnotatedMax, q.AnnotatedPreferred);
        }

        _console.WriteLine();
        _console.Write(table);
    }
}
