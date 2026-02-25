using System.Diagnostics.CodeAnalysis;
using Recyclarr.SyncState;

namespace Recyclarr.Core.Tests.SyncState;

// This class exists because AutoFixture does not use NSubstitute's ForPartsOf()
// See: https://github.com/AutoFixture/AutoFixture/issues/1355
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Created by AutoFixture"
)]
internal sealed class TestSyncStatePersister(ILogger log, ISyncStateStoragePath storagePath)
    : SyncStatePersister(log, storagePath, "test-state")
{
    protected override string DisplayName => "Test State";
}

internal sealed class SyncStatePersisterTest
{
    [Test, AutoMockData]
    public void Load_returns_empty_when_file_does_not_exist(TestSyncStatePersister sut)
    {
        var result = sut.Load();
        result.Mappings.Should().BeEmpty();
    }

    [Test, AutoMockData]
    public void Load_returns_empty_when_json_has_error(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] ISyncStateStoragePath storage,
        TestSyncStatePersister sut
    )
    {
        const string stateJson = """
            {\
              mappings: Hello
            }/
            """;

        fs.AddFile("stateFile.json", new MockFileData(stateJson));
        storage.CalculatePath("test-state").Returns(fs.FileInfo.New("stateFile.json"));

        var result = sut.Load();

        result.Mappings.Should().BeEmpty();
    }

    [Test, AutoMockData]
    public void Load_returns_state_data(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] ISyncStateStoragePath storage,
        TestSyncStatePersister sut
    )
    {
        const string stateJson = """
            {
              "mappings": [
                { "trash_id": "abc", "name": "Test", "service_id": 42 }
              ]
            }
            """;

        fs.AddFile("stateFile.json", new MockFileData(stateJson));
        storage.CalculatePath("test-state").Returns(fs.FileInfo.New("stateFile.json"));

        var result = sut.Load();

        result.Mappings.Should().BeEquivalentTo([new TrashIdMapping("abc", "Test", 42)]);
    }
}
