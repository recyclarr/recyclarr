using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;

namespace Recyclarr.TrashLib.TestLibrary;

public sealed class TestAppPaths : AppPaths
{
    public TestAppPaths(IFileSystem fs)
        : base(fs.CurrentDirectory().SubDirectory("recyclarr"))
    {
    }
}
