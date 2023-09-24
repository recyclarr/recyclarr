using System.IO.Abstractions;
using Recyclarr.Platform;

namespace Recyclarr.Tests.TestLibrary;

public sealed class TestAppPaths : AppPaths
{
    public TestAppPaths(IFileSystem fs)
        : base(fs.CurrentDirectory().SubDirectory("recyclarr"))
    {
    }
}
