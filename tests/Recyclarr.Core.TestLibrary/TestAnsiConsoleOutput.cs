using System.Text;
using Spectre.Console;

namespace Recyclarr.Core.TestLibrary;

/// <summary>
/// Spectre.Console output that writes to Console.Out.
/// TUnit automatically intercepts Console output and associates it with the current test.
/// </summary>
public sealed class TestAnsiConsoleOutput : IAnsiConsoleOutput
{
    public TextWriter Writer => Console.Out;
    public bool IsTerminal => false;
    public int Width => 120;
    public int Height => 24;

    public void SetEncoding(Encoding encoding) { }
}
