using Spectre.Console;

namespace Recyclarr.Cli.Console.Helpers;

internal static class AnsiConsoleExtensions
{
    extension(IAnsiConsole console)
    {
        // Bypasses Spectre rendering, which folds long lines at the console width and would
        // split machine-readable records across lines.
        public void WriteRawLine(string value)
        {
            console.Profile.Out.Writer.WriteLine(value);
        }
    }
}
