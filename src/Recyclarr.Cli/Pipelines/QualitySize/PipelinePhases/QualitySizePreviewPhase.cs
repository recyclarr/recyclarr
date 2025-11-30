using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

internal class QualitySizePreviewPhase(IAnsiConsole console)
    : PreviewPipelinePhase<QualitySizePipelineContext>
{
    protected override void RenderPreview(QualitySizePipelineContext context)
    {
        var table = new Table();

        table.Title("Quality Sizes [red](Preview)[/]");
        table.AddColumn("[bold]Quality[/]");
        table.AddColumn("[bold]Min[/]");
        table.AddColumn("[bold]Max[/]");
        table.AddColumn("[bold]Preferred[/]");

        // Do not check ConfigOutput for null here since the Config Phase checks that for us
        foreach (var q in context.Qualities)
        {
            var quality = $"[dodgerblue1]{q.Item.Quality}[/]";
            table.AddRow(quality, q.AnnotatedMin, q.AnnotatedMax, q.AnnotatedPreferred);
        }

        console.WriteLine();
        console.Write(table);
    }
}
