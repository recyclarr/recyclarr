using System.Text;
using NUnit.Framework;
using Spectre.Console;

namespace Recyclarr.Core.TestLibrary;

public sealed class NUnitAnsiConsoleOutput : IAnsiConsoleOutput
{
    public TextWriter Writer => TestContext.Out;
    public bool IsTerminal => false;
    public int Width => 120;
    public int Height => 24;

    public void SetEncoding(Encoding encoding) { }
}
