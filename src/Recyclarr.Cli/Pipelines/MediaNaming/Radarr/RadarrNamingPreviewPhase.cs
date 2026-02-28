using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.MediaNaming.Radarr;

internal class RadarrNamingPreviewPhase(IAnsiConsole console)
    : PreviewPipelinePhase<RadarrNamingPipelineContext>(console)
{
    protected override void RenderPreview(RadarrNamingPipelineContext context)
    {
        RenderTitle(context);

        var data = context.TransactionOutput;
        var table = new Table().AddColumns("[b]Field[/]", "[b]Value[/]");
        AddRow(table, "Enable Movie Renames?", data.RenameMovies);
        AddRow(table, "Movie", data.StandardMovieFormat);
        AddRow(table, "Folder", data.MovieFolderFormat);
        Console.Write(table);
    }

    private static void AddRow(Table table, string field, object? value)
    {
        table.AddRow(field.EscapeMarkup(), value?.ToString().EscapeMarkup() ?? "UNSET");
    }
}
