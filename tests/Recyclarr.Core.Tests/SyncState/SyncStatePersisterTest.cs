using System.Diagnostics.CodeAnalysis;
using Recyclarr.SyncState;

namespace Recyclarr.Core.Tests.SyncState;

[SyncStateName("test-state")]
internal sealed record TestStateObject : SyncStateObject, ITrashIdMappings
{
    public string? ExtraData { [UsedImplicitly] get; init; }
    public List<TrashIdMapping> Mappings { get; init; } = [];
}

// This class exists because AutoFixture does not use NSubstitute's ForPartsOf()
// See: https://github.com/AutoFixture/AutoFixture/issues/1355
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Created by AutoFixture"
)]
internal sealed class TestSyncStatePersister(ILogger log, ISyncStateStoragePath storagePath)
    : SyncStatePersister<TestStateObject>(log, storagePath)
{
    protected override string StateName => "Test State";
}

internal sealed class SyncStatePersisterTest
{
    [Test, AutoMockData]
    public void Load_returns_default_when_file_does_not_exist(TestSyncStatePersister sut)
    {
        var result = sut.Load();
        result.StateObject.Should().BeEquivalentTo(new TestStateObject());
    }

    [Test, AutoMockData]
    public void Load_returns_default_when_json_has_error(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] ISyncStateStoragePath storage,
        TestSyncStatePersister sut
    )
    {
        const string stateJson = """
            {\
              extra_data: Hello
            }/
            """;

        fs.AddFile("stateFile.json", new MockFileData(stateJson));
        storage.CalculatePath<TestStateObject>().Returns(fs.FileInfo.New("stateFile.json"));

        var result = sut.Load();

        result.StateObject.Should().BeEquivalentTo(new TestStateObject());
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
              "extra_data": "Hello"
            }
            """;

        fs.AddFile("stateFile.json", new MockFileData(stateJson));
        storage.CalculatePath<TestStateObject>().Returns(fs.FileInfo.New("stateFile.json"));

        var result = sut.Load();

        result.StateObject.Should().BeEquivalentTo(new TestStateObject { ExtraData = "Hello" });
    }
}
