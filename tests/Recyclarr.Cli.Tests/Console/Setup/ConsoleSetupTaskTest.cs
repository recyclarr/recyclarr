using Recyclarr.Cli.Console.Commands;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Console.Setup;
using Spectre.Console.Cli;
using Spectre.Console.Testing;

namespace Recyclarr.Cli.Tests.Console.Setup;

internal sealed class ConsoleSetupTaskTest
{
    [Test]
    public void Log_mode_disables_interactive_console()
    {
        using var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        var settings = new BaseCommandSettings
        {
            LogLevel = new FlagValue<CliLogLevel> { IsSet = true, Value = CliLogLevel.Info },
        };

        new ConsoleSetupTask(console).OnStart(settings);

        console.Profile.Capabilities.Interactive.Should().BeFalse();
    }
}
