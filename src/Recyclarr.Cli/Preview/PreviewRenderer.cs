using Recyclarr.Sync;
using Spectre.Console;

namespace Recyclarr.Cli.Preview;

internal class PreviewRenderer(IAnsiConsole console, ISyncJobResults results)
{
    public void Render(JobId jobId, IReadOnlyList<string> instanceNames)
    {
        foreach (var instanceName in instanceNames)
        {
            console.WriteLine();
            console.Write(new Rule($"[bold]{instanceName.EscapeMarkup()}[/]").LeftJustified());

            var instanceResult = results.GetInstanceResult(jobId, instanceName);

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
