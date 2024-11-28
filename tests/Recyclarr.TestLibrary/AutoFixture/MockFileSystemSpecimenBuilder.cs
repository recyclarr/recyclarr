using System.IO.Abstractions;
using AutoFixture;

namespace Recyclarr.TestLibrary.AutoFixture;

public class MockFileSystemSpecimenBuilder : ICustomization
{
    private static int _mockPathCounter;

    public void Customize(IFixture fixture)
    {
        var fs = new MockFileSystem();
        fixture.Inject(fs);

        fixture.Customize<IFileInfo>(x =>
            x.FromFactory(() =>
            {
                var name = $"MockFile-{_mockPathCounter}";
                Interlocked.Increment(ref _mockPathCounter);
                return fs.CurrentDirectory().File(name);
            })
        );

        fixture.Customize<IDirectoryInfo>(x =>
            x.FromFactory(() =>
            {
                var name = $"MockDirectory-{_mockPathCounter}";
                Interlocked.Increment(ref _mockPathCounter);
                return fs.CurrentDirectory().SubDirectory(name);
            })
        );
    }
}
