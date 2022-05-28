using System.IO.Abstractions;

namespace Recyclarr.Tests.Migration.Steps;

public class TestAppPaths : AppPaths
{
    public string BasePath { get; }

    public TestAppPaths(IFileSystem fs)
        : base(fs)
    {
        BasePath = fs.Path.Combine("base", "path");
        SetAppDataPath(fs.Path.Combine(BasePath, DefaultAppDataDirectoryName));
    }
}
