using Recyclarr.Cli.Console.Helpers;
using Spectre.Console;

namespace Recyclarr.Cli.Tests.Console.Helpers;

internal sealed class AnsiConsoleExtensionsTest
{
    [Test]
    public void Raw_line_is_not_wrapped_to_console_width()
    {
        using var output = new StringWriter();
        var console = AnsiConsole.Create(
            new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.No,
                ColorSystem = ColorSystemSupport.NoColors,
                Interactive = InteractionSupport.No,
                Out = new AnsiConsoleOutput(output),
            }
        );
        console.Profile.Width = 10;
        const string line = "trash-id\tA long custom format name\tCategory";

        console.WriteRawLine(line);

        output.ToString().Should().Be(line + Environment.NewLine);
    }
}
