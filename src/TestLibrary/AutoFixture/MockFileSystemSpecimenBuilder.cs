using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using System.IO.Abstractions.TestingHelpers;
using AutoFixture;

namespace TestLibrary.AutoFixture;

public class MockFileSystemSpecimenBuilder : ICustomization
{
    public void Customize(IFixture fixture)
    {
        var fs = new MockFileSystem();
        fixture.Inject(fs);

        fixture.Customize<IFileInfo>(x => x.FromFactory(() =>
        {
            var name = $"MockFile-{fixture.Create<string>()}";
            return fs.CurrentDirectory().File(name);
        }));

        fixture.Customize<IDirectoryInfo>(x => x.FromFactory(() =>
        {
            var name = $"MockDirectory-{fixture.Create<string>()}";
            return fs.CurrentDirectory().SubDirectory(name);
        }));
    }
}
