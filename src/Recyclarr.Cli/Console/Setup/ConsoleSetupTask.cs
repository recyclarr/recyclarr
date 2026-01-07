using Recyclarr.Cli.Console.Commands;
using Recyclarr.Cli.Console.Helpers;
using Spectre.Console;

namespace Recyclarr.Cli.Console.Setup;

internal class ConsoleSetupTask(IAnsiConsole console, ProgressFactory progressFactory)
    : IGlobalSetupTask
{
    public void OnStart(BaseCommandSettings cmd)
    {
        // Log mode: redirect console output to null (Serilog takes over)
        if (cmd.LogLevel.IsSet)
        {
            console.Profile.Out = new AnsiConsoleOutput(TextWriter.Null);
        }

        // Raw mode: configure for clean TSV output
        if (cmd is ListCommandSettings { Raw: true })
        {
            console.Profile.Capabilities.Interactive = false;
            progressFactory.UseSilentFallback = true;
        }
    }

    public void OnFinish() { }
}
