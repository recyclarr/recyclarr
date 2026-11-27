using System.Text;
using Spectre.Console;

namespace Recyclarr.Core.TestLibrary;

public sealed class TestAnsiConsoleOutput : IAnsiConsoleOutput
{
    // TUnit intercepts Console.Out and routes each write to the output of the running test.
    public TextWriter Writer => Console.Out;
    public bool IsTerminal => false;
    public int Width => 120;
    public int Height => 24;

    public void SetEncoding(Encoding encoding) { }
}
