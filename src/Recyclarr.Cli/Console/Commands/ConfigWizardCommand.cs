using System.ComponentModel;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Console.Wizard;
using Recyclarr.Cli.Processors;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("Launch an interactive wizard to create a configuration file.")]
internal class ConfigWizardCommand(
    WizardApplication wizardApp,
    ProviderProgressHandler providerProgressHandler
) : AsyncCommand<ConfigWizardCommand.CliSettings>
{
    [UsedImplicitly]
    internal class CliSettings : BaseCommandSettings;

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        CliSettings settings,
        CancellationToken ct
    )
    {
        // Load guide data before Terminal.Gui takes over the terminal
        await providerProgressHandler.InitializeProvidersAsync(silent: false, ct);

        wizardApp.Run();
        return (int)ExitStatus.Succeeded;
    }
}
