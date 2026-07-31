using Recyclarr.Client.V1;
using Spectre.Console;

namespace Recyclarr.Cli.Preview;

internal class PreviewRenderer(IAnsiConsole console)
{
    public void Render(GetSyncJobResultsResponse results)
    {
        foreach (var instanceResult in results.Instances)
        {
            var instanceName = instanceResult.Instance;

            console.WriteLine();
            console.Write(new Rule($"[bold]{instanceName.EscapeMarkup()}[/]").LeftJustified());

            if (instanceResult.CustomFormats is { } cf)
            {
                RenderHeader("Custom Formats", instanceName);
                CustomFormatPreviewRenderer.Render(console, cf);
            }

            if (instanceResult.QualityProfiles is { } qp)
            {
                RenderHeader("Quality Profiles", instanceName);
                QualityProfilePreviewRenderer.Render(console, qp);
            }

            if (instanceResult.QualitySizes is { } qs)
            {
                RenderHeader("Quality Sizes", instanceName);
                QualitySizePreviewRenderer.Render(console, qs);
            }

            if (instanceResult.SonarrNaming is { } sonarrNaming)
            {
                RenderHeader("Sonarr Media Naming", instanceName);
                SonarrNamingPreviewRenderer.Render(console, sonarrNaming);
            }

            if (instanceResult.RadarrNaming is { } radarrNaming)
            {
                RenderHeader("Radarr Media Naming", instanceName);
                RadarrNamingPreviewRenderer.Render(console, radarrNaming);
            }

            if (instanceResult.MediaManagement is { } mm)
            {
                RenderHeader("Media Management", instanceName);
                MediaManagementPreviewRenderer.Render(console, mm);
            }
        }
    }

    private void RenderHeader(string description, string instanceName)
    {
        console.WriteLine();
        console.MarkupLine(
            $"── [bold]{description}[/] [red](Preview)[/] [dim][[{instanceName.EscapeMarkup()}]][/] ──"
        );
    }
}
