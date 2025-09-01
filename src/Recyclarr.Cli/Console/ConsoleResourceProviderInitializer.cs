using Recyclarr.Cli.Logging;
using Recyclarr.ResourceProviders;
using Spectre.Console;

namespace Recyclarr.Cli.Console;

internal class ConsoleResourceProviderInitializer(
    IAnsiConsole console,
    ResourceProviderProcessor processor
)
{
    public async Task InitializeAllProviders(
        IConsoleOutputSettings outputSettings,
        CancellationToken token = default
    )
    {
        var task = processor.ProcessResourceProviders(token);

        if (outputSettings.IsRawOutputEnabled)
        {
            await task;
            return;
        }

        await console.Status().StartAsync("Initializing Resource Providers...", _ => task);
    }
}
