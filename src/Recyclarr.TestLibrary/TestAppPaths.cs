using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;

namespace Recyclarr.TestLibrary;

public sealed class TestAppPaths : AppPaths
{
    public TestAppPaths(IFileSystem fs)
        : base(fs.CurrentDirectory().SubDirectory("recyclarr"))
    {
    }
}
