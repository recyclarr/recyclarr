using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.Console.Helpers;

internal class ProgressFactory(IAnsiConsole console)
{
    public bool UseSilentFallback { get; set; }

    public Progress Create()
    {
        var progress = console.Progress();
        if (UseSilentFallback)
        {
            progress.UseRenderHook((_, _) => new EmptyRenderable());
        }
        return progress;
    }

    private sealed class EmptyRenderable : IRenderable
    {
        public Measurement Measure(RenderOptions options, int maxWidth) => new(0, 0);

        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) => [];
    }
}
