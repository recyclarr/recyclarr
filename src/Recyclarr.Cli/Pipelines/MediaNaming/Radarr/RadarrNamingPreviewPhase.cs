using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.MediaNaming.Radarr;

internal class RadarrNamingPreviewPhase(IAnsiConsole console)
    : PreviewPipelinePhase<RadarrNamingPipelineContext>(console)
{
    protected override void RenderPreview(RadarrNamingPipelineContext context)
    {
        RenderTitle(context);

        var dto = context.TransactionOutput;
        var table = new Table().AddColumns("[b]Field[/]", "[b]Value[/]");
        AddRow(table, "Enable Movie Renames?", dto.RenameMovies);
        AddRow(table, "Movie", dto.StandardMovieFormat);
        AddRow(table, "Folder", dto.MovieFolderFormat);
        Console.Write(table);
    }

    private static void AddRow(Table table, string field, object? value)
    {
        table.AddRow(field.EscapeMarkup(), value?.ToString().EscapeMarkup() ?? "UNSET");
    }
}
