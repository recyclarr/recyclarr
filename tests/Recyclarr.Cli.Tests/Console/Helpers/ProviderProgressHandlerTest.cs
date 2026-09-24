using System.IO.Abstractions;
using Autofac;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Tests.Reusable;
using Spectre.Console;

namespace Recyclarr.Cli.Tests.Console.Helpers;

internal sealed class ProviderProgressHandlerTest : CliIntegrationFixture
{
    private readonly StringWriter _output = new();

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        base.RegisterStubsAndMocks(builder);

        builder
            .Register(_ =>
                AnsiConsole.Create(
                    new AnsiConsoleSettings
                    {
                        Ansi = AnsiSupport.No,
                        ColorSystem = ColorSystemSupport.NoColors,
                        Interactive = InteractionSupport.No,
                        Out = new AnsiConsoleOutput(_output),
                    }
                )
            )
            .As<IAnsiConsole>()
            .SingleInstance();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _output.Dispose();
        }

        base.Dispose(disposing);
    }

    [Test]
    public async Task Silent_initialization_writes_nothing_when_providers_succeed()
    {
        var sut = Resolve<ProviderProgressHandler>();

        await sut.InitializeProvidersAsync(silent: true, CancellationToken.None);

        _output.ToString().Should().BeEmpty();
    }

    [Test]
    public async Task Silent_initialization_reports_provider_failure()
    {
        Fs.AddFile(
            Paths.ConfigDirectory.File("settings.yml"),
            new MockFileData(
                """
                resource_providers:
                  - name: missing-provider
                    type: custom-formats
                    service: radarr
                    path: does-not-exist
                """
            )
        );

        var sut = Resolve<ProviderProgressHandler>();

        await sut.InitializeProvidersAsync(silent: true, CancellationToken.None);

        _output.ToString().Should().Contain("Failed: missing-provider");
    }
}
