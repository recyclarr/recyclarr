using System.IO.Abstractions;

namespace Recyclarr.TrashLib.TestLibrary;

public sealed class TestAppPaths : AppPaths
{
    public TestAppPaths(IFileSystem fs)
        : base(fs.CurrentDirectory().SubDirectory("recyclarr"))
    {
    }
}
