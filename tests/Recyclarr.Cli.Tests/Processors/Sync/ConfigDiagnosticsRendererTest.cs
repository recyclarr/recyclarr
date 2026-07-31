using Recyclarr.Cli.Processors.Sync;
using Recyclarr.Client.V1;
using Spectre.Console.Testing;

namespace Recyclarr.Cli.Tests.Processors.Sync;

internal sealed class ConfigDiagnosticsRendererTest
{
    [Test]
    public void Render_writes_every_diagnostic_variant_to_console()
    {
        using var console = new TestConsole();
        var diagnostics = new ConfigDiagnosticsResponse
        {
            MissingConfigFiles = ["missing.yml"],
            ParseFailures = [new() { FileName = "invalid.yml", Message = "invalid yaml" }],
            UnknownInstances = ["unknown"],
            AvailableInstances = ["available"],
            InvalidInstances = [new() { InstanceName = "invalid", Errors = ["invalid instance"] }],
            DuplicateInstances = ["duplicate"],
            SplitInstanceGroups =
            [
                new() { BaseUrl = "http://localhost", InstanceNames = ["split"] },
            ],
            DeprecationWarnings = ["deprecated"],
        };

        new ConfigDiagnosticsRenderer(console).Render(diagnostics);

        string[] expected =
        [
            "missing.yml",
            "invalid.yml",
            "invalid yaml",
            "unknown",
            "available",
            "invalid",
            "invalid instance",
            "duplicate",
            "http://localhost",
            "split",
            "deprecated",
        ];
        console.Output.Should().ContainAll(expected);
    }

    [Test]
    public void Render_does_not_log_available_instances_without_unknown_instances()
    {
        using var console = new TestConsole();
        var diagnostics = new ConfigDiagnosticsResponse { AvailableInstances = ["hidden"] };

        new ConfigDiagnosticsRenderer(console).Render(diagnostics);

        console.Output.Should().NotContain("hidden");
    }
}
