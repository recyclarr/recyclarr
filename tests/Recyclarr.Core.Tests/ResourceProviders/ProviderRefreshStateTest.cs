using System.IO.Abstractions;
using Recyclarr.ResourceProviders.Storage;

namespace Recyclarr.Core.Tests.ResourceProviders;

internal sealed class ProviderRefreshStateTest
{
    private readonly MockFileSystem _fs = new();

    // Round-trip and missing-file reads are covered by ResourceCoordinatorTest.
    [Test]
    public void Corrupt_file_reads_as_no_state()
    {
        var file = _fs.CurrentDirectory().File("corrupt.state.json");
        _fs.AddFile(file, new MockFileData("{ not json"));

        ProviderRefreshState.Read(file).Should().BeNull();
    }
}
