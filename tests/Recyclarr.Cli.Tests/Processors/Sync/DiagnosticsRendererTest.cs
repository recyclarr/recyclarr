using Recyclarr.Cli.Processors.Sync;
using Recyclarr.Client.V1;
using Spectre.Console.Testing;

namespace Recyclarr.Cli.Tests.Processors.Sync;

internal sealed class DiagnosticsRendererTest
{
    [Test]
    public void Render_writes_every_diagnostic_level_to_console()
    {
        using var console = new TestConsole();
        var diagnostics = new IReadOnlyListOfDiagnosticEventResponse
        {
            new()
            {
                Level = "Error",
                Message = "failed",
                Instance = "radarr",
            },
            new() { Level = "Warning", Message = "warning" },
            new() { Level = "Deprecation", Message = "deprecated" },
            new() { Level = "FutureLevel", Message = "future" },
        };

        new DiagnosticsRenderer(console).Render(diagnostics);

        console.Output.Should().ContainAll("failed", "warning", "deprecated", "future", "radarr");
    }
}
